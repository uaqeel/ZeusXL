using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra.Double;


namespace CommonTypes.Maths
{
    public class PolynomialInterpolator
    {
        int Degree { get; set; }
        public double[] Coefficients { get; set; }


        public PolynomialInterpolator(int degree)
        {
            Degree = degree;
        }


        public void Fit(double[] xData, double[] yData)
        {
            int nData = xData.Length;

            Matrix monomials = new DenseMatrix(nData, Degree + 1);
            for (int r = 0; r < nData; ++r)
            {
                for (int c = 0; c < Degree + 1; ++c)
                {
                    monomials[r, c] = Math.Pow(xData[r], c);
                }
            }

            Coefficients = monomials.QR().Solve(new DenseVector(yData)).ToArray();
        }


        public double GetValue(double x)
        {
            double y = Coefficients[0];
            for (int i = 1; i < Degree + 1; ++i)
            {
                y += Coefficients[i] * Math.Pow(x, i);
            }

            return y;
        }
    }
}
