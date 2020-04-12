using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes.Maths
{
    class BSpline
    {
        int Order;
        bool MakeEndPointsExtrema;
        bool UseBezierEndConditions;

        double[] X;
        double[] Y;

        double[] LocalMeanX;
        double[] LocalMeanY;
        int[] KnotIndices;
        int NumKnots;


        // Note that order == 4 means a cubic B-spline.
        public BSpline(int order, bool makeEndPointsExtrema, bool useBezierEndConditions)
        {
            Order = order;
            MakeEndPointsExtrema = makeEndPointsExtrema;
            UseBezierEndConditions = useBezierEndConditions;
        }


        // Leave m_makeEndPointsExtrema = true. m_useBezierEndConditions = true makes
        // end-points have the same multiplicity as the order of the spline. 
        public BSpline(int order) : this(order, true, true)
        {
        }


        public void Build(double[] xData, double[] yData) {
            // First of all, increase the multiplicity of yData[0] and yData.last() to m_order.
            int nDataInitial = xData.Length, nData = nDataInitial;

            if (UseBezierEndConditions) {
                double xSpacing = xData[1] - xData[0];								// Arbitrary attempt at scaling to data....
                nData = nDataInitial + 2 * (Order - 1);
                X = new double[nData];
                Y = new double[nData];

                for (int i = 0; i < nData; ++i) {
                    if (i < Order - 1) {
                        X[i] = xData[0] - (Order - i - 1) * xSpacing;
                        Y[i] = yData[0];
                    }
                    else if (i >= nDataInitial + Order - 1) {
                        X[i] = xData.Last() + (i - nDataInitial - Order + 2) * xSpacing;
                        Y[i] = yData.Last();
                    }
                    else {
                        X[i] = xData[i - Order + 1];
                        Y[i] = yData[i - Order + 1];
                    }
                }
            } else {
                X = xData;
                Y = yData;
            }

            // Now calculate the initial set of knots.
            getExtrema(Y);

            LocalMeanX = new double[nDataInitial];
            LocalMeanY = new double[nDataInitial];

            int index = 0;
            for (int t = Order - 1; t <= nData - Order; ++t) {
                double localMean = 0.0, basis = 0.0;

                while (KnotIndices[index] <= t) index++;

                // Given that x[m_knotIndices[index]] <= x[t] < x[m_knotIndices[index+1]], we need to calculate basis
                // functions B[index-k+1], ..., B[index].
                for (int j = 0; j < Order; ++j) {
                    int l = index - Order + j;

                    // Secret sauce for the last point -- order decay.
                    if (l + Order == NumKnots)
                        continue;

                    if (Order == 4) {
                        basis = (X[KnotIndices[l+Order]] - X[KnotIndices[l]]) *
                                DividedDifferences.DD4(X[t], X[KnotIndices[l]], X[KnotIndices[l+1]], X[KnotIndices[l+2]], X[KnotIndices[l+3]], X[KnotIndices[l+4]]);

                        localMean += 0.25 * (Y[KnotIndices[l+1]] + 2 * Y[KnotIndices[l+2]] + Y[KnotIndices[l+3]]) * basis;
                    } else if (Order == 3) {
                        basis = (X[KnotIndices[l+Order]] - X[KnotIndices[l]]) *
                            DividedDifferences.DD3(X[t], X[KnotIndices[l]], X[KnotIndices[l+1]], X[KnotIndices[l+2]], X[KnotIndices[l+3]], 3);

                        localMean += 0.5 * (Y[KnotIndices[l+1]] + Y[KnotIndices[l+2]]) * basis;
                    } else {
                        throw new Exception("Error, b-spline only implemented for orders 3 and 4!");
                    }
                }

                LocalMeanX[t - Order + 1] = X[t];
                LocalMeanY[t - Order + 1] = localMean;
            }
        }


        public double GetLocalMean(double xValue) {
            int index = Array.FindIndex(LocalMeanX, x => x > xValue) - 1;

            if (index >= 0)
                return LocalMeanY[index];

            return double.NaN;
        }


        public double[] GetLocalMeans() {
            return LocalMeanY;
        }


        public double[] GetLocalMeans(double[] xData) {
            int n = xData.Length;
            double[] localMeans = new double[n];

            for (int i = 0; i < n; ++i)
                localMeans[i] = GetLocalMean(xData[i]);

            return localMeans;
        }


        void getExtrema(double[] yData) {
            // First clear the old set of knots.
            NumKnots = 0;
            KnotIndices = new int[yData.Length];

            double z, pz, nz;

            pz = yData[0];
            z = yData[1];

            if (MakeEndPointsExtrema) {
                KnotIndices[NumKnots++] = 0;
            }

            int nData = yData.Length;
            for (int i = 1; i < nData - 1; ++i) {
                nz = yData[i+1];

                if (z <= nz && z <= pz) {
                    // This is a local minimum.
                    KnotIndices[NumKnots++] = i;
                }
                else if (z >= nz && z >= pz) {
                    // This is a local maximum.
                    KnotIndices[NumKnots++] = i;
                }

                pz = z;
                z = nz;
            }

            if (MakeEndPointsExtrema)
                KnotIndices[NumKnots++] = yData.Length - 1;
        }
    }
}
