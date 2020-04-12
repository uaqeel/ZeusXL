using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Accord.Math.Decompositions;


namespace CommonTypes.Maths
{
    public class OptimalRiskDecomposition
    {
        public int MaxRiskFactors;
        public string[] InputLabels;
        public double[,] InputCorrelationConfiguration;

        public string[] OptimalLabels;
        public double[,] OptimalCholeskyDecomposition;

        private int r, n;
        private int pivotIndex, swapIndex;
        private int iteration;
        private double[,] optimalCorrelationConfiguration;
        private double bestScore;


        public OptimalRiskDecomposition(string[] Labels, double[,] Correlations, int maxRiskFactors)
        {
            MaxRiskFactors = Math.Min(maxRiskFactors, Labels.Length);

            InputLabels = Labels;
            InputCorrelationConfiguration = Correlations;

            // Total of r instruments.
            r = Correlations.GetLength(0);
            if (Labels.Length != r)
                throw new Exception("Error, you must provide a label for each row of the data matrix!");

            // But we'll only permute r - 1 rows.
            n = r - 1;

            OptimalLabels = (string[])InputLabels.Clone();
        }


        public object[,] Compute()
        {
            int[] indices = new int[n];
            for (int i = 0; i < n; ++i)
                indices[i] = i;

            bestScore = double.MinValue;
            int[] bestPermutation = indices;

            pivotIndex = 0;
            swapIndex = 0;
            iteration = 0;

            int[] permutation = null;
            while (Permute(bestPermutation, out permutation))
            {
                double[,] permutedData = new double[r, r];

                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j <= i; ++j)
                    {
                        permutedData[i, j] = permutedData[j, i] = InputCorrelationConfiguration[permutation[i], permutation[j]];
                    }
                }

                for (int j = 0; j < n; ++j)
                {
                    permutedData[n, j] = permutedData[j, n] = InputCorrelationConfiguration[n, permutation[j]];
                }

                permutedData[n, n] = InputCorrelationConfiguration[n, n];

                double[,] cholesky = new CholeskyDecomposition(permutedData).LeftTriangularFactor;

                double score = Math.Pow(cholesky[r - 1, pivotIndex], 2);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPermutation = permutation.ToArray();
                }

                iteration++;
            }

            optimalCorrelationConfiguration = new double[r, r];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j <= i; ++j)
                {
                    optimalCorrelationConfiguration[i, j] = optimalCorrelationConfiguration[j, i] = InputCorrelationConfiguration[bestPermutation[i], bestPermutation[j]];
                }
            }

            for (int j = 0; j < n; ++j)
            {
                optimalCorrelationConfiguration[n, j] = optimalCorrelationConfiguration[j, n] = InputCorrelationConfiguration[n, bestPermutation[j]];
            }

            optimalCorrelationConfiguration[n, n] = InputCorrelationConfiguration[n, n];

            OptimalCholeskyDecomposition = new CholeskyDecomposition(optimalCorrelationConfiguration).LeftTriangularFactor;

            object[,] ret = new object[r + 1, r + 1];
            ret[0, 0] = "";
            for (int i = 0; i < n; ++i)
            {
                OptimalLabels[i] = InputLabels[bestPermutation[i]];
                ret[0, i + 1] = ret[i + 1, 0] = OptimalLabels[i];

                for (int j = 0; j < r; ++j)
                {
                    ret[i + 1, j + 1] = OptimalCholeskyDecomposition[i, j];
                }
            }

            OptimalLabels[n] = InputLabels[n];
            ret[0, n + 1] = ret[n + 1, 0] = OptimalLabels[n];
            for (int j = 0; j < r; ++j)
            {
                ret[n + 1, j + 1] = OptimalCholeskyDecomposition[n, j];
            }

            return ret;
        }


        // Could do the permutation in place....
        public bool Permute(int[] list, out int[] permutation)
        {
            if (list == null || list.Length == 0 || pivotIndex == MaxRiskFactors)
            {
                permutation = null;
                return false;
            }

            permutation = (int[])list.Clone();

            if (swapIndex > list.Length - 1)
            {
                pivotIndex++;
                swapIndex = pivotIndex;
                bestScore = double.MinValue;
            }

            if (swapIndex > pivotIndex && pivotIndex < list.Length - 1)
            {
                int temp = permutation[swapIndex];
                permutation[swapIndex] = permutation[pivotIndex];
                permutation[pivotIndex] = temp;
            }

            // Debug.WriteLine(string.Join(",", permutation));

            swapIndex++;
            return true;
        }


        private void swap(ref int x, ref int y)
        { x = x ^ y; y = y ^ x; x = x ^ y; }
    }
}
