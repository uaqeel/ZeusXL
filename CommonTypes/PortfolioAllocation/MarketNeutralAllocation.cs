using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTypes.Maths;
using MathNet.Numerics;


namespace CommonTypes.PortfolioAllocation
{
    public class MarketNeutralAllocation
    {
        int NumAssets;
        double[][] Cholesky;


        // It's a good idea to make cash the last item in the correlation matrix -- we're going to be doing (essentially) a least-squares
        // solution, so anything we can't neutralise will end up as an exposure in the last asset.
        public MarketNeutralAllocation(double[,] Correlations)
        {
            NumAssets = Correlations.GetLength(0);

            // It would be a good idea to sort this matrix to put cash at the end...
            var corr = MathNet.Numerics.LinearAlgebra.Matrix.Create(Correlations);
            MathNet.Numerics.LinearAlgebra.CholeskyDecomposition cd = new MathNet.Numerics.LinearAlgebra.CholeskyDecomposition(corr);

            Cholesky = cd.GetL().GetArray();
        }


        public double[] Compute()
        {
            double[,] A = new double[NumAssets + 1, NumAssets];
            for (int i = 0; i < NumAssets; ++i)
            {
                for (int j = 0; j < NumAssets; ++j)
                {
                    A[i, j] = Cholesky[j][i];
                }
            }

            for (int j = 0; j < NumAssets; ++j)
            {
                A[NumAssets, j] = 1;
            }

            double[,] Y = new double[NumAssets + 1, 1];                             // Elements = 0 ==> factor exposures.
            Y[NumAssets, 0] = 1;                                                    // Weights sum up to 0.

            var a = MathNet.Numerics.LinearAlgebra.Matrix.Create(A);
            var y = MathNet.Numerics.LinearAlgebra.Matrix.Create(Y);

            var x = a.SolveRobust(y).GetArray();

            double[] output = new double[NumAssets];
            for (int i = 0; i < NumAssets; ++i)
            {
                output[i] = x[i][0];
            }

            return output;
        }
    }
}
