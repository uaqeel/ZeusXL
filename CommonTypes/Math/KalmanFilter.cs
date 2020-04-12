using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra.Double;


namespace CommonTypes.Maths
{
    public class KalmanFilter
    {
        public Matrix X0;                                      // Current estimate of process state.
        public Matrix P0;                                      // Current estimate of process covariance.

        public Matrix F { get; private set; }                  // State transition model for process states.
        public Matrix B { get; private set; }                  // Control input model.
        public Matrix U { get; private set; }                  // Control vector.
        public Matrix Q { get; private set; }                  // Covariance of process noise.
        public Matrix H { get; private set; }                  // Observation model, maps true space into observed space.
        public Matrix R { get; private set; }                  // Covariance of observation noise.

        public Matrix State { get; private set; }              // Current process state.
        public Matrix Covariance { get; private set; }         // Current process covariance.

        public KalmanFilter(double[,] f, double[,] b, double[,] u, double[,] q, double[,] h, double[,] r)
        {
            F = new DenseMatrix(f);
            B = new DenseMatrix(b);
            U = new DenseMatrix(u);
            Q = new DenseMatrix(q);
            H = new DenseMatrix(h);
            R = new DenseMatrix(r);

            int nRows = F.RowCount, nCols = F.ColumnCount;
            State = new DenseMatrix(nRows, nCols);
            Covariance = new DenseMatrix(nRows, nRows);
        }

        public void Predict()
        {
            // A priori state estimate:
            //     x = Fx' + Bu
            X0 = F.Multiply(State).Add(B.Multiply(U)) as Matrix;

            // A priori covariance estimate:
            //     P = FP'F + Q
            P0 = F.Multiply(Covariance).Multiply(F.Transpose()).Add(Q) as Matrix;
        }

        public void Correct(double[,] dz)
        {
            Matrix z = new DenseMatrix(dz);

            // Innovation residual:
            //     S = HP'H + R
            Matrix s = H.Multiply(P0).Multiply(H.Transpose()).Add(R) as Matrix;

            // Optimal Kalman gain:
            //     K = P'HS^-1
            Matrix k = P0.Multiply(H.Transpose()).Multiply(s.Inverse()) as Matrix;

            // A posteriori state estimate:
            //      X = x' + K(z - Hx')
            State = X0.Add(k.Multiply(z.Subtract(H.Multiply(X0)))) as Matrix;

            // A posteriori covariance estimate:
            //      P = (I - kH)P'
            Matrix I = new DiagonalMatrix(P0.RowCount, P0.RowCount, 1);
            Covariance = I.Subtract(k.Multiply(H)).Multiply(P0) as Matrix;
        }

        public void UpdateObservationCovariance(double[,] r)
        {
            R = new DenseMatrix(r);
        }
    }
}
