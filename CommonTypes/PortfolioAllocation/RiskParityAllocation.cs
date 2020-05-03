using System;
using System.Linq;

using CommonTypes.Maths;
using Accord.Math;


namespace CommonTypes.PortfolioAllocation
{
    public class RiskParityAllocation : DifferentialEvolution
    {
        int NumAssets;
        double[] Vols;
        double[,] Corrs;

        double[][] Cholesky;
        bool CashIsRiskLess;                                                        // Good to have one risk-less asset if possible...better results.


        public RiskParityAllocation(double[] Volatilities, double[,] Correlations, bool cashIsRiskLess)
        {
            Vols = Volatilities;
            Corrs = Correlations;
            CashIsRiskLess = cashIsRiskLess;
            NumAssets = Volatilities.Length;

            var corr = MathNet.Numerics.LinearAlgebra.Matrix.Create(Correlations);
            MathNet.Numerics.LinearAlgebra.CholeskyDecomposition cd = new MathNet.Numerics.LinearAlgebra.CholeskyDecomposition(corr);

            Cholesky = cd.GetL().GetArray();

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
            
            Tuple<double, double[]> result = Optimise(-1, bounds, parameterTypes, maxIterations, stoppingEpsilon);
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

            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < NumAssets - (CashIsRiskLess ? 1 : 0); ++i)
            {
                double a = Matrix.ElementwiseMultiply(weights, Vols).InnerProduct(Cholesky.Column(i));

                if (a > max)
                    max = a;
                if (a < min)
                    min = a;
            }

            return Math.Abs(max - min);
        }
    }
}
