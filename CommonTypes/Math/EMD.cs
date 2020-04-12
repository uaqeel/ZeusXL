using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes.Maths
{
    public class EMD
    {
        int Order;
        int MaxIMFs;
        int MaxIterations;

        int NumData;
        BSpline Spline;
        double[][] IMFs;


        public int NumIMFs { get; private set; }                                            // Doesn't include residue.


        public EMD(int order, int maxIMFs, int maxIterations)
        {
            if (order != 3 && order != 4)
                throw new Exception("Error, only cubic and quadratic b-splines implemented!");

            Order = order;
            MaxIMFs = maxIMFs;
            MaxIterations = maxIterations;
            NumIMFs = -1;

            Spline = new BSpline(Order);           
        }


        public void Run(double[] xData, double[] yData)
        {
	        NumData = xData.Length;

	        IMFs = new double[MaxIMFs + 1][];                                             // 1 for the residue.
            NumIMFs = MaxIMFs;
            for (int i = 0; i < MaxIMFs + 1; ++i)
                IMFs[i] = new double[NumData];

	        double[] residue = new double[yData.Length];
            Array.Copy(yData, residue, yData.Length);
	        for (int i = 0; i < MaxIMFs; ++i) {
		        bool stop = Sift(xData, residue, out IMFs[i]);

		        for (int j = 0; j < NumData; ++j)
			        residue[j] -= IMFs[i][j];

                if (stop)
                {
                    NumIMFs = i + 1;
                    break;
                }
	        }

	        IMFs[MaxIMFs] = residue;
        }


        bool Sift(double[] x, double[] y, out double[] imf) {
	        // First copy the current signal into IMF.
	        imf = new double[y.Length];
            Array.Copy(y, imf, y.Length);

	        // Now build the first local mean.
	        Spline.Build(x, y);

	        int nData = x.Length;
	        double[] mean = new double[nData];

	        int i = 0;
	        bool residueMonotonic = true;
	        while (true) {
		        mean = Spline.GetLocalMeans();

		        // Compute the residue.
		        for (int j = 0; j < nData; ++j) {
			        imf[j] -= mean[j];
		        }

		        // Having computed the residue, we are left with 2 tasks:
		        // - compute the number of zero crossings and the number of extrema
		        // - check whether the residue is now monotonic.
		        double nZeroCrossings = 0, nExtrema = 0;
		        for (int j = 1; j < nData - 1; ++j) {
			        double pz = imf[j-1];
			        double z = imf[j];
			        double nz = imf[j+1];

			        if (Math.Sign(pz) != Math.Sign(z))
				        nZeroCrossings++;

			        if (z <= nz && z <= pz || z >= nz && z >= pz)
				        nExtrema++;

			        if (residueMonotonic && (Math.Sign(mean[j] - mean[j-1]) != Math.Sign(mean[j+1] - mean[j])))
				        residueMonotonic = false;
		        }

		        i++;

		        if (Math.Abs(nExtrema - nZeroCrossings) <= 1)
			        break;
		        else if (i >= MaxIterations)
			        break;
		        else
			        Spline.Build(x, imf);
	        }

	        return residueMonotonic;
        }


        //public void Update(double xDatum, double yDatum) {
        //    throw new NotImplementedException("EMD - no update!");
        //}


        public double[] this[int i] {
            get
            {
                return IMFs[i];
            }
        }
    }


    public class DividedDifferences
    {
        static double Function(double t, double x, int k)
        {
            double x_minus_t = x - t;

            if (x_minus_t < 0)
                return 0;
            else
                return Math.Pow(x_minus_t, k);
        }


        public static double DD3(double t, double tau0, double tau1, double tau2, double tau3, int order)
        {
            double s0 = 0, s1 = 0, s2 = 0, s3 = 0;

            double r = 0;
            if ((s3 = Function(t, tau3, order - 1)) > 0)
            {
                if ((s2 = Function(t, tau2, order - 1)) > 0)
                {
                    if ((s1 = Function(t, tau1, order - 1)) > 0)
                    {
                        if ((s0 = Function(t, tau0, order - 1)) > 0)
                        {
                            r = (((s0 - s1) / (tau0 - tau1) - (s1 - s2) / (tau1 - tau2)) / (tau0 - tau2) - ((s1 - s2) / (tau1 - tau2) - (s2 - s3) / (tau2 - tau3)) / (tau1 - tau3)) / (tau0 - tau3);
                        }
                        else
                        {
                            r = (((-s1) / (tau0 - tau1) - (s1 - s2) / (tau1 - tau2)) / (tau0 - tau2) - ((s1 - s2) / (tau1 - tau2) - (s2 - s3) / (tau2 - tau3)) / (tau1 - tau3)) / (tau0 - tau3);
                        }
                    }
                    else
                    {
                        r = ((s2 / (tau1 - tau2)) / (tau0 - tau2) - (-s2 / (tau1 - tau2) - (s2 - s3) / (tau2 - tau3)) / (tau1 - tau3)) / (tau0 - tau3);
                    }
                }
                else
                {
                    r = (-s3 / (tau2 - tau3) / (tau1 - tau3)) / (tau0 - tau3);
                }
            }
            else
            {
                r = 0;
            }

            return r;
        }


        public static double DD4(double t, double tau0, double tau1, double tau2, double tau3, double tau4)
        {
            double f0 = DD3(t, tau0, tau1, tau2, tau3, 4);
            double f1 = DD3(t, tau1, tau2, tau3, tau4, 4);

            double r = (f0 - f1) / (tau0 - tau4);

            return r;
        }
    }
}

