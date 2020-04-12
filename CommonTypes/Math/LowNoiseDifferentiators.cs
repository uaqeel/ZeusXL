using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using CommonTypes;


// From: http://www.holoborodko.com/pavel/numerical-methods/numerical-derivative/smooth-low-noise-differentiators/
// This isn't finished yet, for the following reasons:
// - I can't change the filter length for one-sided differentiatiors
// - I'm using h = "average spacing"
// - I don't have 2nd order derivatives
// http://www.holoborodko.com/pavel/image-processing/edge-detection/
namespace Strategies
{
    public class LowNoiseDifferentiator
    {
        int FilterLength;
        bool OneSided;

        Vector Coefficients;
        CircularBuffer<double> xData, yData;

        double deriv;


        public LowNoiseDifferentiator(int filterLength, bool oneSided)
        {
            OneSided = oneSided;
            if (OneSided)
            {
                Coefficients = HybridOneSidedCoefficients(FilterLength);
                FilterLength = 16;                                                          // Ugh!
            }
            else
            {
                FilterLength = filterLength;
                Coefficients = new DenseVector(TwoSidedCoefficients(FilterLength));
            }

            xData = new CircularBuffer<double>(FilterLength);
            yData = new CircularBuffer<double>(FilterLength);
        }


        public LowNoiseDifferentiator(Dictionary<string, object> config)
            : this(config.GetOrDefault("FilterLength", 15.0).AsInt(), (bool)config.GetOrDefault("OneSided", true))
        {
        }


        public double Update(double x, double y)
        {
            xData.Insert(x);
            yData.Insert(y);

            return Value;
        }


        public double Value
        {
            get
            {
                if (xData.Full)
                {
                    deriv = Coefficients * new DenseVector(yData.Data);
                    deriv /= (xData.Last() - xData.First()) / FilterLength;                                 // Ugh.

                    return deriv;
                }
                else
                {
                    return double.NaN;
                }
            }
        }
        

        // Yes, very hacky.
        private DenseVector HybridOneSidedCoefficients(int FilterLength)
        {
            double[] coeff = new double[] { 203, 98, 25, -50, -93, -138, -151, -166, -149, -134, -87, -42, 35, 110, 217, 322 };
            for (int i = 0; i < coeff.Length; ++i)
            {
                coeff[i] /= 2856;
            }

            return new DenseVector(coeff);
        }


        // These are the two-sided coefficients, so not much use.
        private DenseVector TwoSidedCoefficients(int N)
        {
            if (N % 2 == 0)
                throw new Exception("Error, FilterLength must be odd!");

            int m = (int)(0.5 * (N - 3));
            int M = (int)(0.5 * (N - 1));

            double denominator = 1 / (Math.Pow(2, 2 * m + 1));

            double[] coeff = new double[M];
            for (int k = 0; k < M; ++k)
            {
                coeff[k] = (SpecialFunctions.Binomial(2 * m, m - k + 1) - SpecialFunctions.Binomial(2 * m, m - k - 1)) * denominator;
            }

            return new DenseVector(coeff);
        }
    }
}
