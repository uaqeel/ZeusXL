using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra.Double;


namespace CommonTypes.Maths
{
    // Uses the quadratic representation of Andersen, Bollerslev and Meddahi, so could be used
    // to represent other variance estimators quite easily.
    //
    // Follows http://dx.doi.org/10.1016/j.jeconom.2010.03.032.
    public class KernelRealisedVariance
    {
        int WindowLength;                                                       // In seconds.
        int ReturnInterval;                                                     // In seconds.
        int SamplingInterval;                                                   // In seconds.
        string KernelName;                                                      // Currently just the modified TH2.

        double ScalingFactor;                                                   // We want the estimator to spit out daily vars.

        int NumPeriods;                                                         // The size of the return vector.
        CircularBuffer<double> R_t;                                             // Returns over a period of WindowLength seconds, sampled at SamplingInterval.

        int h_;                                                                 // The interval over which returns are computed.
        int h;                                                                  // The actual sampling interval.
        int L;                                                                  // Bandwidth, calculated using ABM's L = h / h_underline - 1.

        Func<double, double> Kernel;                                            // Kernel delegate.
        Matrix Q;                                                               // This will be NumPeriods x NumPeriods.


        public KernelRealisedVariance(int windowLength, int returnInterval, int samplingInterval, string kernelName)
        {
            WindowLength = windowLength;
            ReturnInterval = returnInterval;
            SamplingInterval = samplingInterval;
            KernelName = kernelName;

            if (SamplingInterval < ReturnInterval)
                throw new ArgumentException(string.Format("Error, sampling interval ({0}) can't be less than the return calculation interval ({1})!",
                                                          SamplingInterval, ReturnInterval));

            if (kernelName == string.Empty || kernelName == "ModifiedTukeyHanningKernel2")
                Kernel = ModifiedTukeyHanningKernel2;
            else
                throw new ArgumentException("Error, unrecognised kernel name: '" + kernelName + "'!");

            // Reminder - my first thought is naively to always say
            //              daily variance = total quadratic variation / window length * 86400
            // but the ReturnInterval in the numerator is necessary otherwise we're measuring
            // the variance of different intervals. For example, excluding ReturnInterval in the numerator,
            //              ReturnInterval = 1 gives you the daily variance of 1-second returns
            //              ReturnInterval = 10 gives you the daily variance of 10-second returns.
            // By including ReturnInterval in the numerator, we are standardising to measuring the
            // variance of 1-second returns.
            ScalingFactor = 86400.0 * ReturnInterval / WindowLength;

            NumPeriods = WindowLength / ReturnInterval;
            R_t = new CircularBuffer<double>(NumPeriods);

            h_ = ReturnInterval;
            h = SamplingInterval;
            L = h / h_ - 1;

            Q = new DenseMatrix(NumPeriods, NumPeriods);
            for (int i = 0; i < NumPeriods; ++i)
            {
                for (int j = 0; j < NumPeriods; ++j)
                {
                    Q[i, j] = q(i, j, L);
                }
            }
        }


        // h == h_.
        public KernelRealisedVariance(int WindowLength, int SamplingInterval, string KernelName)
            : this(WindowLength, SamplingInterval, SamplingInterval, KernelName)
        {
        }


        public void Update(double r)
        {
            R_t.Insert(r);
        }


        // The realised variance scaled to a period of 1 day.
        public double DailyVariance
        {
            get
            {
                double rv = 0;
                if (R_t.Full)
                {
                    Vector r_t = new DenseVector(R_t.Data);
                    rv = Q.LeftMultiply(r_t).DotProduct(r_t) * ScalingFactor;
                }

                return rv;
            }
        }


        // The realised vol scaled to a period of 1 day.
        public double DailyVol
        {
            get
            {
                return Math.Sqrt(DailyVariance);
            }
        }


        // Elements of the Q matrix.
        double q(double i, double j, double L)
        {
            double v = 0;

            if (i == j) {
                v = 1;
            }
            else if (Math.Abs(i - j) <= L)
            {
                double l = Math.Abs(i - j);
                v = Kernel((l - 1) / L);
            }
            else
            {
                v = 0;
            }

            return v;
        }


        double ModifiedTukeyHanningKernel2(double x)
        {
            double v = Math.Sin(Math.PI * (1 - x) * (1 - x) / 2);
            return v * v;
        }
    }
}

// TODO(strategies) - strategy on VIX: sell whenever annualised realised vol is over 40% (mean reversion).
// TODO(strategies) - interesting ideas on a better VIX calculation: http://www.minyanville.com/business-news/markets/articles/255EVIX-255EGSPC-volatility-seinfeld-investing-cboe/8/13/2012/id/43140
// Specifically, volume-weighting to capture insurance buying and alternative weightings to emphasise out-of-the-money vols.