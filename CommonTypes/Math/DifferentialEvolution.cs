using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using CommonTypes;
using MathNet.Numerics.LinearAlgebra.Double;


namespace CommonTypes.Maths
{
    public class DifferentialEvolution
    {
        protected int Dimensionality;
        protected int PopulationSize;

        double DifferentialWeight;
        double CrossoverProbability;

        protected double[,] Agents;
        protected double[] Scores;

        ParallelOptions ParallelOptions;
        SobolRNG InitialisationRNG;
        SobolRNG MutationRNG;
        SobolRNG RecombinationRNG;

        public int Iteration { get; protected set; }
        protected int MaxIndex;

        private Dictionary<double[], bool> PreviousParameterSets;


        public DifferentialEvolution(int dimensionality, int populationSize, double differentialWeight, double crossoverProbability)
        {
            Initialise(dimensionality, populationSize, differentialWeight, crossoverProbability);
        }


        public DifferentialEvolution()
        { }
        

        public void Initialise(int dimensionality, int populationSize, double differentialWeight, double crossoverProbability)
        {
            Dimensionality = dimensionality;
            PopulationSize = populationSize;

            DifferentialWeight = differentialWeight;
            CrossoverProbability = crossoverProbability;

            ParallelOptions = new ParallelOptions();
            ParallelOptions.MaxDegreeOfParallelism = 1;

            // It's not possible to run this in parallel because generations need to run sequentially in
            // order to converge towards good solutions.
            //ParallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

            InitialisationRNG = new SobolRNG(Dimensionality, 777);
            MutationRNG = new SobolRNG(4, 123);
            RecombinationRNG = new SobolRNG(Dimensionality, 456);

            PreviousParameterSets = new Dictionary<double[], bool>();
        }


        public Tuple<double, double[]> Optimise(int MinMaxFlag, double[,] Bounds, string[] parameterTypes, int MaxIterations, double StoppingEpsilon)
        {
            ParameterTypes[] ParameterTypes = ConvertParameterTypes(parameterTypes);
            InitialiseAgents(MinMaxFlag, Bounds, ParameterTypes);

            MaxIndex = Scores.MaxIndex();
            double maxScore = Scores[MaxIndex];                     // Best score of the current generation.
            double prevMaxScore = 0;                                // Best score of the last generation.

            DenseMatrix aMatrix = new DenseMatrix(Agents);
            double volume = aMatrix.Svd(false).S().Aggregate((x, y) => x * y);
            double prevVolume = 0;

            Iteration = 0;
            while (Iteration++ < MaxIterations && Math.Abs(volume - prevVolume) > StoppingEpsilon)
            {
                prevMaxScore = maxScore;
                prevVolume = volume;

                // Create the new generation.
                double[,] generation = CreateGeneration(Bounds, ParameterTypes);

                // This contains the scores for this generation.
                double[] scores = ScoreGeneration(MinMaxFlag, generation);

                for (int i = 0; i < PopulationSize; ++i)
                {
                    // Update the original agent if appropriate.
                    if (!scores.Equals(double.NaN) && scores[i] > Scores[i])
                    {
                        Scores[i] = scores[i];
                        for (int j = 0; j < Dimensionality; ++j)
                            Agents[i, j] = generation[i, j];
                    }
                }

                // These are the best scores of this generation.
                MaxIndex = scores.MaxIndex();
                maxScore = scores[MaxIndex];

                aMatrix = new DenseMatrix(Agents);
                volume = aMatrix.Svd(false).S().Aggregate((x, y) => x * y);
            }

            MaxIndex = Scores.MaxIndex();
            return new Tuple<double, double[]>(Scores[MaxIndex], Agents.Row(MaxIndex));
        }


        // Either override this or override ScoreGeneration.
        public virtual double ObjectiveFunction(double[] values)
        {
            throw new NotImplementedException("DifferentialEvolution base class doesn't specify an objective function!");
        }


        public double[,] CreateGeneration(double[,] Bounds, ParameterTypes[] VariableType)
        {
            double[,] generation = new double[PopulationSize, Dimensionality];

            Parallel.For(0, PopulationSize, ParallelOptions, i =>
            {
                double[] trial = null;
                while (true)
                {
                    trial = CreateTrial(Bounds, VariableType, i);
                    if (!PreviousParameterSets.ContainsKey(trial))
                    {
                        PreviousParameterSets.Add(trial, false);
                        break;
                    }
                }

                for (int j = 0; j < Dimensionality; ++j)
                {
                    generation[i, j] = trial[j];
                }
            });

            return generation;
        }


        // Either override this or override ObjectiveFunction.
        public virtual double[] ScoreGeneration(int MinMaxFlag, double[,] generation)
        {
            double[] scores = new double[PopulationSize];
            Parallel.For(0, PopulationSize, ParallelOptions, i =>
            {
                double[] trial = new double[Dimensionality];
                for (int j = 0; j < Dimensionality; ++j)
                {
                    trial[j] = generation[i, j];
                }

                // Score the trial.
                scores[i] = MinMaxFlag * ObjectiveFunction(trial);
            });

            return scores;
        }


        public double[] CreateTrial(double[,] Bounds, ParameterTypes[] VariableType, int i)
        {
            // Select the 3 mutation agents and a recombination index.
            double[] randoms = MutationRNG.Next();
            for (int j = 0; j < 3; ++j)
            {
                randoms[j] = Math.Floor(randoms[j] * PopulationSize);
            }

            randoms[3] = Math.Floor(randoms[3] * Dimensionality);

            // Randoms for creating the trial.
            double[] recombinationRandoms = RecombinationRNG.Next();

            // Mutate and create the donor agent.
            double[] donor = new double[Dimensionality];
            for (int j = 0; j < Dimensionality; ++j)
            {
                donor[j] = Agents[(int)randoms[0], j] + DifferentialWeight * (Agents[(int)randoms[1], j] - Agents[(int)randoms[2], j]);
                donor[j] = Math.Min(Math.Max(donor[j], Bounds[j, 0]), Bounds[j, 1]);
            }

            // Recombine with the original agent to get the new trial.
            double[] trial = new double[Dimensionality];
            for (int j = 0; j < Dimensionality; ++j)
            {
                trial[j] = AdjustValueForVariableType(VariableType[j], ((recombinationRandoms[j] < CrossoverProbability || j == randoms[3]) ? donor[j] : Agents[i, j]));
            }

            return trial;
        }


        void InitialiseAgents(int MinMaxFlag, double[,] Bounds, ParameterTypes[] VariableType)
        {
            Agents = new double[PopulationSize, Dimensionality];
            Scores = new double[PopulationSize];

            SobolRNG initialisationRNG = new SobolRNG(Dimensionality, 777);

            double[] randoms;
            for (int i = 0; i < PopulationSize; ++i)
            {
                while (true)
                {
                    randoms = initialisationRNG.Next();

                    for (int j = 0; j < Dimensionality; ++j)
                    {
                        Agents[i, j] = AdjustValueForVariableType(VariableType[j], randoms[j] * Bounds[j, 0] + (1 - randoms[j]) * Bounds[j, 1]);
                    }

                    if (!PreviousParameterSets.ContainsKey(Agents.Row(i)))
                    {
                        PreviousParameterSets.Add(Agents.Row(i), true);
                        break;
                    }
                }
            }

            Scores = ScoreGeneration(MinMaxFlag, Agents);

            for (int i = 0; i < PopulationSize; ++i)
            {
                if (double.IsNaN(Scores[i]))
                {
                    StringBuilder sb = new StringBuilder("Error initialising differential evolution! (");
                    for (int j = 0; j < Dimensionality - 1; ++j) {
                        sb.Append(Agents[i, j] + ",");
                    }

                    sb.Append(Agents[i, Dimensionality - 1] + ")");

                    throw new Exception(sb.ToString());
                }
            }
        }


        public double[,] GetAgents()
        {
            return Agents;
        }


        public double[] GetScores()
        {
            return Scores;
        }


        public double AdjustValueForVariableType(ParameterTypes ParameterType, double value)
        {
            if (ParameterType == ParameterTypes.Real)
                return Math.Round(value, 5);
            else if (ParameterType == ParameterTypes.Integer)
                return (int)(value + 0.5);
            else if (ParameterType == ParameterTypes.Sign)
                return (value <= 0 ? -1 : 1);

            return value;
        }


        private ParameterTypes[] ConvertParameterTypes(string[] parameterTypes)
        {
            ParameterTypes[] ret = new ParameterTypes[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                ret[i] = (ParameterTypes)Enum.Parse(typeof(ParameterTypes), parameterTypes[i]);
            }

            return ret;
        }
    }


    public enum ParameterTypes : int
    {
        Real = 0,
        Integer = 1,
        Sign = 2                                                // That is, +1 or -1.
    }


    public class Sinc2DOptimizer : DifferentialEvolution
    {
        public Sinc2DOptimizer(int dimensionality, int populationSize, double differentialWeight, double crossoverProbability)
            : base(dimensionality, populationSize, differentialWeight, crossoverProbability)
        {
        }

        public override double ObjectiveFunction(double[] values)
        {
            double d = (Math.Sin(values[0]) * Math.Sin(values[1])) / (values[0] * values[1]);

            return d;
        }
    }
}

// Convergence criterion: matrix volume. Reference: http://benisrael.net/VOLUME.pdf