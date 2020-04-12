using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTypes;
using CommonTypes.Maths;


namespace CommonTypes
{
    public class KalmanSmoother : Indicator
    {
        KalmanFilter KF;
        int WindowLength;                                       // Length of the window over which the trailing vol is calculated.
        double Multiplier;                                      // The multiple of the vol to use for the measurement error.
        CircularBuffer<double> Window;                          // The window over which the vol is calculated.

        double[,] z, ad;


        public KalmanSmoother(int SmoothingWindowLength, double SmoothingMultiplier)
        {
            WindowLength = SmoothingWindowLength;
            Multiplier = SmoothingMultiplier;

            double[,] f = new double[1, 1]; f[0, 0] = 1;
            double[,] b = new double[1, 1]; b[0, 0] = 0;        // No control model.
            double[,] u = new double[1, 1]; u[0, 0] = 0;        // No control model.
            double[,] q = new double[1, 1]; q[0, 0] = 1;
            double[,] h = new double[1, 1]; h[0, 0] = 1;

            // Very high initial value for measurement error. Would ideally be of the same scale as the data.
            double[,] r = new double[1, 1]; r[0, 0] = 1e6;

            KF = new KalmanFilter(f, b, u, q, h, r);
            Window = new CircularBuffer<double>(WindowLength);

            // Permanent temporaries.
            z = new double[1, 1];
            ad = new double[1, 1];
        }


        public KalmanSmoother(Dictionary<string, object> config)
            : this(config.GetOrDefault("WindowLength", 20.0).AsInt(), config.GetOrDefault("SmoothingMultiplier", 1.0).AsDouble())
        {
        }


        public override double Update(double newValue)
        {
            // Initialise the KF with the first data value we get.
            if (Window.Length == 0)
                KF.State[0, 0] = newValue;

            // Prediction step.
            KF.Predict();

            // Vol calculation, and update if ready.
            Window.Insert(newValue);
            ad[0, 0] = Multiplier * Window.AD();
            if (Window.Full)
                KF.UpdateObservationCovariance(ad);

            // Update step.
            z[0, 0] = newValue;
            KF.Correct(z);

            return KF.State[0, 0];
        }


        public override double Value {
            get
            {
                return KF.State[0, 0];
            }
        }


        public double VarEstimate
        {
            get
            {
                return KF.Covariance[0, 0];
            }
        }
    }
}

// TODO(research) - interesting: http://www.lanl.gov/DLDSTP/fast/prl_paper_revised.pdf