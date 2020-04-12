using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTypes.Maths;
using Accord.Math;


namespace CommonTypes.PortfolioAllocation
{
    public class OptimalSharpeAllocation : DifferentialEvolution
    {
        int NumAssets;
        double[] Drifts;
        double[,] VolDiagonal;
        double[,] Corrs;


        public OptimalSharpeAllocation(double[] AssetDrifts, double[] Volatilities, double[,] Correlations)
        {
            Drifts = AssetDrifts;
            VolDiagonal = Matrix.Diagonal(Volatilities);
            Corrs = Correlations;
            NumAssets = Drifts.Length;

            var corr = MathNet.Numerics.LinearAlgebra.Matrix.Create(Correlations);

            // Initialise DE.
            Initialise(NumAssets, 1000, 0.5, 0.5);
        }


        public double[] Compute()
        {
            string[] parameterTypes = new string[NumAssets];
            double[,] bounds = new double[NumAssets, 2];

            int maxIterations = 100;
            double stoppingEpsilon = 1e-4;

            for (int i = 0; i < NumAssets; ++i)
            {
                parameterTypes[i] = "Real";
                bounds[i, 0] = -1;
                bounds[i, 1] = 1;
            }

            Tuple<double, double[]> result = Optimise(+1, bounds, parameterTypes, maxIterations, stoppingEpsilon);
            double weightTotal = result.Item2.Sum();

            double[] output = new double[NumAssets];
            for (int i = 0; i < NumAssets; ++i)
            {
                output[i] = result.Item2[i] / weightTotal;
            }

            return output;
        }


        public override double ObjectiveFunction(double[] weights)
        {
            double weightTotal = weights.Sum();

            for (int i = 0; i < NumAssets; ++i)
            {
                weights[i] /= weightTotal;
            }

            double[] left = weights.Multiply(VolDiagonal);
            double[] right = VolDiagonal.Multiply(weights);

            double returns = weights.InnerProduct(Drifts);
            double covar = left.Multiply(Corrs).InnerProduct(right);

            if (covar <= 0)
                return 0;

            return returns / Math.Sqrt(covar);
        }
    }
}
