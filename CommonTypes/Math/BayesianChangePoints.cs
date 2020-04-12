using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using MathNet.Numerics.LinearAlgebra;


namespace CommonTypes
{
    public class BayesianChangePoints
    {
        double[] X;
        double[] Y;
        int NumData;

        public int ChangePointIndex;
        public double Constant;
        public double Trend1;
        public double Trend2;

        Matrix ss;

        public BayesianChangePoints(double[] x, double[] y)
        {
            X = x;
            Y = y;
            NumData = x.Length;

            ChangePointIndex = -1;
        }


        public int Test()
        {
            double minNorm = 1e10;
            ChangePointIndex = -1;

            for (int i = 1; i < NumData - 1; ++i)
            {
                double score = Score(i);

                if (minNorm > score)
                {
                    ChangePointIndex = i;
                    minNorm = score;

                    Constant = ss[0, 0];
                    Trend1 = ss[1, 0];
                    Trend2 = ss[2, 0] + Trend1;
                }
            }

            return ChangePointIndex;
        }


        private double Score(double i)
        {
            double[,] x = new double[NumData, 3];
            for (int j = 0; j < NumData; ++j)
            {
                x[j, 0] = 1;
                x[j, 1] = X[j];
                x[j, 2] = Math.Max(0, j - i);
            }

            var a = Matrix.Create(x);
            var y = new Matrix(Y, NumData);
            ss = a.SolveRobust(y);

            var z = y - a * ss;

            return z.Norm1();
        }
    }
}
