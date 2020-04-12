using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ExcelDna.Integration;
using Accord.MachineLearning;
using CommonTypes;
using Strategies;
using CommonTypes.Maths;


namespace XL
{
    partial class XL
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Optimise option strategy")]
        public static object OptimiseOptionStrategy(object[] CallPutFuture, object[] Strikes, double RequiredRangeLowerBound, double RequiredRangeUpperBound)
        {
            CallPut[] cp = Utils.GetVector<double>(CallPutFuture).Select(x => (CallPut)x).ToArray();
            double[] strikes = Utils.GetVector<double>(Strikes);
            OptionsDiffEvo de = new OptionsDiffEvo(cp, strikes,
                                                    RequiredRangeLowerBound,
                                                    RequiredRangeUpperBound);

            de.Initialise(strikes.Length, 2000, .5, .5);

            double[,] bounds = new double[strikes.Length, 2];
            string[] paramTypes = new string[strikes.Length];
            for (int i = 0; i < strikes.Length; i++) {
                bounds[i, 0] = -100;
                bounds[i, 1] = 100;
                paramTypes[i] = "Integer";
            }



            DateTime start = DateTime.Now;
            Tuple<double, double[]> results = de.Optimise(1, bounds, paramTypes, 1000, 1e-10);
            double runtime = (DateTime.Now - start).TotalMilliseconds;
            Debug.WriteLine("DE Iterations: " + (de.Iteration - 1) + ", Runtime: " + runtime + "ms");

            double[,] agents = de.GetAgents();
            double[] scores = de.GetScores();

            double[,] ret = new double[1 + 1000, 1+strikes.Length];
            ret[0, 0] = results.Item1;
            for (int i = 0; i < strikes.Length; i++)
            {
                ret[0, 1 + i] = results.Item2[i];
            }

            for (int i = 1; i < 1 + 1000; ++i)
            {
                ret[i, 0] = scores[i - 1];

                for (int j = 0; j < strikes.Length; j++)
                {
                    ret[i, 1 + j] = agents[i - 1, j];
                }
            }

            return ret;
        }
    }


    class OptionsDiffEvo : DifferentialEvolution
    {
        double lowerBound, upperBound, step;
        double reqLowerBound, reqUpperBound;
        CallPut[] cp;
        double[] strikes;

        public OptionsDiffEvo(CallPut[] CallPutFuture, double[] Strikes, double RequiredRangeLowerBound, double RequiredRangeUpperBound)
        {
            cp = CallPutFuture;

            strikes = Strikes;
            reqLowerBound = RequiredRangeLowerBound;
            reqUpperBound = RequiredRangeUpperBound;

            lowerBound = reqLowerBound * .999;
            upperBound = reqUpperBound * 1.001;
            step = (upperBound - lowerBound) / 100;
        }

        public override double ObjectiveFunction(double[] values)
        {
            double valueAtLowerBound = CalculateValue(reqLowerBound, values);
            double valueAtUpperBound = CalculateValue(reqUpperBound, values);
            if (valueAtLowerBound < 0 || valueAtUpperBound < 0)
                return 0;

            double[] payoffs = new double[100];
            for (int i = 0; i < 100; i++)
            {
                payoffs[i] = CalculateValue(lowerBound + i * step, values);
            }

            int lower = -1, upper = -1;
            for (int i = 1; i < 99; i++)
            {
                if (payoffs[i - 1] < 0 && payoffs[i] >= 0)
                {
                    lower = i;
                }

                if (payoffs[i - 1] > 0 && payoffs[i] <= 0)
                {
                    upper = i;
                }
            }

            double score = 0;
            if (lower != -1 && upper != -1 && upper > lower)
            {
                double integral = 0;
                for (int i = lower; i < upper; i++)
                    integral += payoffs[i] * step;

                score = integral * payoffs.Max() / values.Select(x => Math.Abs(x)).Sum();
                score *= (upper - lower) * step;
            }

            return score;
        }

        private double CalculateValue(double x, double[] values)
        {
            double value = 0;
            for (int j = 0; j < strikes.Length; j++)
            {
                if (cp[j] == CallPut.Forward)
                    value += values[j] * (x - strikes[j]);
                else if (cp[j] == CallPut.Put)
                    value += values[j] * Math.Max(strikes[j] - x, 0);
                else if (cp[j] == CallPut.Call)
                    value += values[j] * Math.Max(0, x - strikes[j]);
            }

            return value;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Clusters parameter sets and displays stats for scores within each cluster")]
        public static object ClusterParameterSets(double[,] Data, double[] Scores)
        {
            int nRows = Data.GetLength(0), nCols = Data.GetLength(1);
            int numClusters = (int)Math.Ceiling(Math.Log(nRows));

            double[][] data = new double[nRows][];
            for (int r = 0; r < nRows; ++r)
            {
                data[r] = new double[nCols];
                for (int c = 0; c < nCols; ++c)
                {
                    data[r][c] = Data[r, c];
                }
            }

            KMeans km = new KMeans(numClusters);
            int[] labels = km.Compute(data);

            // Calculate the averages and stdevs of the cluster scores.
            SummaryContainer[] summaries = new SummaryContainer[numClusters];
            for (int i = 0; i < labels.Length; ++i)
            {
                if (summaries[labels[i]] == null)
                    summaries[labels[i]] = new SummaryContainer();

                summaries[labels[i]].Add(Scores[i]);
            }

            double[,] ret = new double[numClusters, nCols + 6];
            for (int r = 0; r < numClusters; ++r)
            {
                for (int c = 0; c < nCols; ++c)
                {
                    ret[r, c] = km.Clusters[r].Mean[c];
                }

                ret[r, nCols] = summaries[r].Count;
                ret[r, nCols + 1] = summaries[r].Min;
                ret[r, nCols + 2] = summaries[r].Max;
                ret[r, nCols + 3] = summaries[r].Average;
                ret[r, nCols + 4] = summaries[r].Stdev;
                ret[r, nCols + 5] = summaries[r].Z;
            }

            return ret;
        }
    }
}
