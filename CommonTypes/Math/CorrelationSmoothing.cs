using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes
{
    public static class CorrelationSmoothing
    {
        public static double[,] RMT(double[,] CorrelationMatrix, int NumEigenValuesToKeep)
        {
            Accord.Math.Decompositions.EigenvalueDecomposition evd = new Accord.Math.Decompositions.EigenvalueDecomposition(CorrelationMatrix);

            double[] eValues = evd.RealEigenvalues;
            double[,] eVectors = evd.Eigenvectors;

            int nEigen = eValues.Length;
            int[] keys = Enumerable.Range(0, nEigen).ToArray();
            double[] values = (double[])eValues.Clone();
            Array.Sort(keys, values);

            int n = Math.Min(nEigen, NumEigenValuesToKeep);

            double x = 0;
            for (int i = nEigen - 1; i > nEigen - 1 - n; --i)
            {
                x += values[i];
            }

            double replacementValue = (nEigen - x) / (nEigen - NumEigenValuesToKeep);
            for (int i = nEigen - n - 1; i >= 0; --i)
            {
                values[i] = replacementValue;
            }

            double check = 0;
            for (int i = 0; i < nEigen; ++i)
            {
                check += values[i];
            }

            double[,] reconstructedMatrix = new double[nEigen, nEigen];
            for (int i = 0; i < nEigen; ++i)
            {
                for (int j = 0; j < nEigen; ++j)
                {
                    for (int k = 0; k < nEigen; ++k)
                    {
                        reconstructedMatrix[i, j] += values[k] * eVectors[i, keys[k]] * eVectors[j, keys[k]];
                    }
                }
            }

            double[,] ret = new double[nEigen, nEigen];
            for (int i = 0; i < nEigen; ++i)
            {
                for (int j = 0; j < nEigen; ++j)
                {
                    ret[i, j] = reconstructedMatrix[i, j] / Math.Sqrt(reconstructedMatrix[i, i] * reconstructedMatrix[j, j]);
                }
            }

            return ret;
        }
    }
}
