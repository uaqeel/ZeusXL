using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

using ExcelDna.Integration;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Analysis;
using AForge.Math;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.Interpolation.Algorithms;
using CommonTypes;
using CommonTypes.Maths;
using CommonTypes.PortfolioAllocation;
using Strategies;


namespace XL
{
    public class XLMathFunctions
    {
        [ExcelFunction(Category = "ZeusXL", Description = "1-d Kalman filter smoother")]
        public static object KalmanSmoother(double[] Data, object SmoothingCoefficientOpt, object SmoothingWindowOpt)
        {
            double smoothingCoefficient = Utils.GetOptionalParameter(SmoothingCoefficientOpt, -1.0);
            int smoothingWindow = (int)Utils.GetOptionalParameter(SmoothingWindowOpt, 20.0);

            KalmanSmoother ks = new KalmanSmoother(smoothingWindow, smoothingCoefficient);
            int i = 0;
            double[,] ret = new double[Data.Length, 2];
            foreach (double d in Data)
            {
                ret[i, 0] = ks.Update(d);
                ret[i, 1] = ks.VarEstimate;

                i++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Empirical Mode Decomposition")]
        public static object EMD(double[] xData, double[] yData, object MaxIMFsOpt, object MaxIterationsOpt, object OrderOpt)
        {
            int maxIMFs = (int)Utils.GetOptionalParameter(MaxIMFsOpt, 5.0);
            int maxIters = (int)Utils.GetOptionalParameter(MaxIterationsOpt, 5.0);
            int order = (int)Utils.GetOptionalParameter(OrderOpt, 4.0);

            CommonTypes.Maths.EMD emd = new EMD(order, maxIMFs, maxIters);
            emd.Run(xData, yData);

            int nData = xData.Length;
            double[,] ret = new double[nData, maxIMFs + 1];
            for (int i = 0; i < maxIMFs + 1; ++i)
            {
                double[] temp = emd[i];

                for (int j = 0; j < nData; ++j)
                {
                    ret[j, maxIMFs - i] = temp[j];
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Hilbert Transform")]
        public static object FHT(double[] Data, object BackwardFlagOpt)
        {
            bool backwards = Utils.GetOptionalParameter(BackwardFlagOpt, false);
            FourierTransform.Direction direction = (backwards ? FourierTransform.Direction.Backward : FourierTransform.Direction.Forward);

            try
            {
                HilbertTransform.FHT(Data, direction);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            double[,] ret = new double[Data.Length, 1];
            for (int i = 0; i < Data.Length; ++i)
                ret[i, 0] = Data[i];

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Fourier Transform")]
        public static object FFT(double[] Data, object BackwardFlagOpt)
        {
            bool backwards = Utils.GetOptionalParameter(BackwardFlagOpt, false);
            FourierTransform.Direction direction = (backwards ? FourierTransform.Direction.Backward : FourierTransform.Direction.Forward);

            Complex[] cData = new Complex[Data.Length];
            for (int i = 0; i < Data.Length; ++i)
                cData[i] = new Complex(Data[i], 0);

            try
            {
                FourierTransform.FFT(cData, direction);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            double[,] ret = new double[Data.Length, 1];
            for (int i = 0; i < Data.Length; ++i)
                ret[i, 0] = cData[i].Re;

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Kernel realised variance estimator")]
        public static object KernelRealisedVariance(double[] Data, int WindowLength, int ReturnInterval, int SamplingInterval)
        {
            KernelRealisedVariance krv = new KernelRealisedVariance(WindowLength, ReturnInterval, SamplingInterval, string.Empty);

            double[,] ret = new double[Data.Length - WindowLength, 1];
            for (int i = 0; i < Data.Length; ++i)
            {
                krv.Update(Data[i]);

                if (i >= WindowLength)
                    ret[i - WindowLength, 0] = krv.DailyVariance;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Root-mean-squared for an array")]
        public static object RMS(double[] Data)
        {
            double rms = MathUtils.RMS(Data);
            return rms;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "K-means clustering")]
        public static object KCluster(double[,] Data, int NumClusters)
        {
            int nRows = Data.GetLength(0), nCols = Data.GetLength(1);

            double[][] data = new double[nRows][];
            for (int r = 0; r < nRows; ++r)
            {
                data[r] = new double[nCols];
                for (int c = 0; c < nCols; ++c)
                {
                    data[r][c] = Data[r, c];
                }
            }

            // Unfortunately, you can't set the initial cluster centers manually here, which
            // means that every time this function is called, it will return different clusters.
            KMeans km = new KMeans(NumClusters);
            int[] labels = km.Compute(data);

            int nClusters = km.Clusters.Count;
            double[,] ret = new double[nClusters, nCols];
            int x = 0;
            foreach (KMeansCluster kmc in km.Clusters.OrderBy(c => c.Mean[0]))
            {
                for (int c = 0; c < nCols; ++c)
                {
                    ret[x, c] = kmc.Mean[c];
                }

                x++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Singular value decomposition")]
        public static object SVD(double[,] Data)
        {
            DenseMatrix data = new DenseMatrix(Data);
            int nRows = data.RowCount, nCols = data.ColumnCount;

            MathNet.Numerics.LinearAlgebra.Double.Factorization.Svd svd = data.Svd(true);

            Matrix<double> u = svd.U();     // m x m
            Matrix<double> w = svd.W();     // m x n, diagonal
            Matrix<double> vt = svd.VT();   // n x n, transpose

            object[,] ret = new object[Math.Max(nRows, nCols), nRows + 1 + nCols + 1 + nCols];
            for (int i = 0; i < ret.GetLength(0); ++i)
            {
                for (int j = 0; j < ret.GetLength(1); ++j)
                {
                    ret[i, j] = "";
                }
            }

            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nRows; ++j)
                {
                    ret[i, j] = u[i, j];
                }
            }

            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    ret[i, nRows + 1 + j] = w[i, j];
                }
            }

            for (int i = 0; i < nCols; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    ret[i, nRows + 1 + nCols + 1 + j] = vt[j, i];                   // Re-transpose, so that they're in the expected format.
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Eigendecomposition")]
        public static object EVD(double[,] Data)
        {

            int nRows = Data.GetLength(0), nCols = Data.GetLength(1);
            if (nRows != nCols)
                throw new Exception("Error, eigendecomposition is only defined for square matrices!");

            Accord.Math.Decompositions.EigenvalueDecomposition evd = new Accord.Math.Decompositions.EigenvalueDecomposition(Data);
            double[,] values = evd.DiagonalMatrix;
            double[,] vectors = evd.Eigenvectors;

            nRows = Math.Max(values.GetLength(0), vectors.GetLength(0));
            nCols = values.GetLength(1) + 1 + vectors.GetLength(1);

            object[,] ret = new object[nRows, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < values.GetLength(1); ++j)
                {
                    if (i < values.GetLength(0))
                        ret[i, j] = values[i, j];
                }

                ret[i, values.GetLength(1)] = "";

                for (int j = 0; j < vectors.GetLength(1); ++j)
                {
                    if (i < vectors.GetLength(0))
                        ret[i, j + values.GetLength(1) + 1] = vectors[i, j];
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Cholesky decomposition")]
        public static object Cholesky(double[,] Data)
        {
            try
            {
                Accord.Math.Decompositions.CholeskyDecomposition cd = new Accord.Math.Decompositions.CholeskyDecomposition(Data);
                return cd.LeftTriangularFactor;
            }
            catch (Exception e)
            {
                return "Error, dimension mismatch! Check if the matrix is square." + e.ToString();
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "QR decomposition")]
        public static object QRD(double[,] Data)
        {
            try
            {
                int nRows = Data.GetLength(0), nCols = Data.GetLength(1);
                Accord.Math.Decompositions.QrDecomposition cd = new Accord.Math.Decompositions.QrDecomposition(Data);

                object[,] ret = new object[nRows, 2 * nCols + 1];
                for (int i = 0; i < nRows; ++i)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        ret[i, j] = cd.OrthogonalFactor[i, j];
                    }

                    ret[i, nCols] = "";

                    for (int j = 0; j < nCols; ++j)
                    {
                        ret[i, j + nCols + 1] = cd.UpperTriangularFactor[i, j];
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                return "Error, dimension mismatch! Check if the matrix is square." + e.ToString();
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Invert matrix")]
        public static object InvertMatrix(double[,] Data)
        {
            DenseMatrix data = new DenseMatrix(Data);

            try
            {
                if (data.Determinant() == 0)
                    throw new Exception("Error, input matrix is singular!");

                return data.Inverse().ToArray();
            }
            catch (Exception e)
            {
                MathNet.Numerics.LinearAlgebra.Double.Factorization.Evd evd = data.Evd();
                if (evd.Determinant == 0)
                    return e.ToString();

                Matrix<double> ev = evd.EigenVectors();
                var i_evalues = new DiagonalMatrix(evd.D());
                int nEigenvectors = ev.ColumnCount;
                for (int i = 0; i < nEigenvectors; ++i)
                {
                    i_evalues[i, i] = 1 / i_evalues[i, i];
                }

                var inverse = ev * i_evalues * ev.Transpose();
                return inverse.ToArray();
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Matrix determinant")]
        public static object Determinant(double[,] Data)
        {
            DenseMatrix data = new DenseMatrix(Data);

            try
            {
                return data.Determinant();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Perform principal component analysis on a data set")]
        public static object PCA(double[,] Data, object MethodOpt)
        {
            string method = Utils.GetOptionalParameter(MethodOpt, "Center");

            AnalysisMethod am = AnalysisMethod.Center;
            if (method != "Center")
                am = AnalysisMethod.Standardize;

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(Data, am);
            pca.Compute();

            int nRows = pca.ComponentProportions.Length, nCols = pca.ComponentMatrix.GetLength(1);
            object[,] ret = new object[nRows + 1, nCols + 2];
            for (int i = 0; i < nCols + 2; ++i) ret[0, i] = "";
            ret[0, 0] = "Component Proportions";
            ret[0, 2] = "Components";

            for (int i = 0; i < nRows; ++i)
            {
                ret[i + 1, 0] = pca.ComponentProportions[i];
                ret[i + 1, 1] = "";

                for (int j = 0; j < nCols; ++j)
                {
                    ret[i + 1, j + 2] = pca.ComponentMatrix[i, j];
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns the covariance matrix for a data set")]
        public static object CovarianceMatrix(object[,] Data)
        {
            double[,] data = Utils.GetMatrix<double>(Data);
            return data.Covariance();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns the correlation matrix for a data set, or just the correlation for a pair")]
        public static object CorrelationMatrix(object[,] Data, object Index1Opt, object Index2Opt)
        {
            int index1 = Utils.GetOptionalParameter(Index1Opt, -1);
            int index2 = Utils.GetOptionalParameter(Index2Opt, -1);

            double[,] data = Utils.GetMatrix<double>(Data);
            var corrs = Accord.Statistics.Tools.Correlation(data);

            if (index1 > -1 && index2 > -1)
                return corrs[index1, index2];
            else
                return corrs;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Variance-minimising Cholesky decomposition")]
        public static object OptimalRiskDecomposition(double[,] Data, object[] Labels, object MaxRiskFactorsOpt)
        {
            int maxRiskFactors = Utils.GetOptionalParameter(MaxRiskFactorsOpt, Labels.Length - 1);
            string[] labels = Labels.Select(x => x.ToString()).ToArray();

            CommonTypes.Maths.OptimalRiskDecomposition ord = new CommonTypes.Maths.OptimalRiskDecomposition(labels, Data, maxRiskFactors);

            return ord.Compute();
        }

        [ExcelFunction(Category = "ZeusXL", Description = "Autocorrelations by lag")]
        public static object AutoCorrelations(double[,] Data, object BackwardTooOpt)
        {
            bool backwardsToo = Utils.GetOptionalParameter(BackwardTooOpt, false);

            int nData = Data.GetLength(0), nVars = Data.GetLength(1);
            if (nVars != 2)
                throw new Exception("Error, Autocorrelations currently only calculated for 1 pair!");

            object[,] ret = new object[nData, 3];
            ret[0, 0] = "Lag";
            ret[0, 1] = "NumData";
            ret[0, 2] = "Correlation";

            double[] independent = Data.Column(0);
            double[] dependent = Data.Column(1);
            for (int i = 0; i < nData - 1; ++i)
            {
                double[] x = independent.Submatrix(0, nData - 1 - i);
                double[] y = dependent.Submatrix(i, nData - 1);
                DependenceContainer sc = new DependenceContainer();

                for (int j = 0; j < x.Length; ++j)
                {
                    sc.Add(x[j], y[j]);
                }

                ret[i + 1, 0] = i;
                ret[i + 1, 1] = x.Length;
                ret[i + 1, 2] = sc.Correlation;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "LUD decomposition")]
        public static object LUD(double[,] Data)
        {
            try
            {
                int nRows = Data.GetLength(0), nCols = Data.GetLength(1);
                Accord.Math.Decompositions.LuDecomposition cd = new Accord.Math.Decompositions.LuDecomposition(Data);

                object[,] ret = new object[nRows, 2 * nCols + 1];
                for (int i = 0; i < nRows; ++i)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        ret[i, j] = cd.LowerTriangularFactor[i, j];
                    }

                    ret[i, nCols] = "";

                    for (int j = 0; j < nCols; ++j)
                    {
                        ret[i, j + nCols + 1] = cd.UpperTriangularFactor[i, j];
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                return "Error, dimension mismatch! Check if the matrix is square." + e.ToString();
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns the diagonal elements of a matrix")]
        public static object Diagonal(double[,] Data)
        {
            return Data.Diagonal();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Turns a vector into a diagonal matrix")]
        public static object MakeDiagonal(double[] Data)
        {
            return Accord.Math.Matrix.Diagonal(Data);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Smooths correlations using Random Matrix Theory")]
        public static object SmoothCorrelationMatrix(double[,] Correlations, int NumEigenValuesToKeep)
        {
            return CorrelationSmoothing.RMT(Correlations, NumEigenValuesToKeep);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Solve a system of linear equations")]
        public static object Solve(double[,] Y, double[,] A)
        {
            var a = MathNet.Numerics.LinearAlgebra.Matrix.Create(A);
            var y = MathNet.Numerics.LinearAlgebra.Matrix.Create(Y);

            var x = a.SolveRobust(y).GetArray();

            return x.ToMatrix();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Fit a polynomial to data")]
        public static object FitPolynomial(double[] Y, double[] X, object DegreeOpt)
        {
            int degree = Utils.GetOptionalParameter(DegreeOpt, 1);

            PolynomialInterpolator pi = new PolynomialInterpolator(degree);
            pi.Fit(X, Y);

            return pi.Coefficients;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get a market-neutral portfolio allocation")]
        public static object ComputeMarketNeutralAllocation(double[,] CorrelationMatrix)
        {
            MarketNeutralAllocation mna = new MarketNeutralAllocation(CorrelationMatrix);

            double[] result = mna.Compute();
            return result;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get a risk parity portfolio allocation")]
        public static object ComputeRiskParityAllocation(object[] Volatilities, object[,] CorrelationMatrix, object CashIsRiskLessOpt, object ReturnAllSolutionsOpt)
        {
            double[] vols = Utils.GetVector<double>(Volatilities);
            double[,] corrs = Utils.GetMatrix<double>(CorrelationMatrix);

            bool cashIsRiskLess = Utils.GetOptionalParameter(CashIsRiskLessOpt, true);
            bool returnAllSolutions = Utils.GetOptionalParameter(ReturnAllSolutionsOpt, false);

            RiskParityAllocation rpa = new RiskParityAllocation(vols, corrs, cashIsRiskLess);

            double[] result = rpa.Compute();

            if (!returnAllSolutions)
            {
                return result;
            }
            else
            {
                object[,] all = new object[1001, vols.Length + 1];
                all.FillWithEmptyStrings();

                all[0, 0] = "Abs(Max factor vol - Min factor vol)";

                double[] scores = rpa.GetScores();
                double[,] agents = rpa.GetAgents();

                for (int i = 0; i < 1000; ++i)
                {
                    all[i + 1, 0] = scores[i];

                    double normaliser = agents.Row(i).Sum();
                    for (int j = 0; j < Volatilities.Length; ++j)
                    {
                        all[i + 1, j + 1] = agents[i, j] / normaliser;
                    }
                }

                return all;
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get an optimal Sharpe ratio portfolio allocation")]
        public static object ComputeOptimalSharpeAllocation(object[] Drifts, object[] Volatilities, object[,] CorrelationMatrix, object ReturnAllSolutionsOpt)
        {
            bool returnAllSolutions = Utils.GetOptionalParameter(ReturnAllSolutionsOpt, false);

            double[] drifts = Utils.GetVector<double>(Drifts);
            double[] vols = Utils.GetVector<double>(Volatilities);
            double[,] corrs = Utils.GetMatrix<double>(CorrelationMatrix);

            OptimalSharpeAllocation osa = new OptimalSharpeAllocation(drifts, vols, corrs);

            double[] result = osa.Compute();

            if (!returnAllSolutions)
            {
                return result;
            }
            else
            {
                object[,] all = new object[1001, vols.Length + 1];
                all.FillWithEmptyStrings();

                all[0, 0] = "Sharpe";

                double[] scores = osa.GetScores();
                double[,] agents = osa.GetAgents();

                for (int i = 0; i < 1000; ++i)
                {
                    all[i + 1, 0] = scores[i];

                    double normaliser = agents.Row(i).Sum();
                    for (int j = 0; j < Volatilities.Length; ++j)
                    {
                        all[i + 1, j + 1] = agents[i, j] / normaliser;
                    }
                }

                return all;
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Value a set of options")]
        public static object BSValuation(double Spot, object[] DivYields, object[] Vols, object[] Ts, object[] DFs, object[] Strikes, object[] PutCalls, string ValuationOpt, object SpotShiftOpt, object TimeShiftOpt)
        {
            double[] divs = Utils.GetVector<double>(DivYields);
            double[] vols = Utils.GetVector<double>(Vols);
            double[] ts = Utils.GetVector<double>(Ts);
            double[] dfs = Utils.GetVector<double>(DFs);
            double[] strikes = Utils.GetVector<double>(Strikes);
            double[] putcalls = Utils.GetVector<double>(PutCalls);
            string val = Utils.GetOptionalParameter(ValuationOpt, "Premium");
            double spotShift = Utils.GetOptionalParameter(SpotShiftOpt, 0.0);
            double timeShift = Utils.GetOptionalParameter(TimeShiftOpt, 0.0);

            int nOptions = vols.Length;
            if (/*nOptions != ts.Length || */nOptions != dfs.Length || nOptions != strikes.Length || nOptions != putcalls.Length)
                return "ERROR: inconsistent data lengths!";

            double[] values = new double[nOptions];

            for (int i = 0; i < nOptions; ++i)
            {
                double value = 0;
                if (ts[i] <= 0)
                {
                    value = 0;
                }
                else {
                    CallPut cp = (CallPut)putcalls[i];

                    double adjustedSpot = Spot * (1 + spotShift);
                    double adjustedT = ts[i] - timeShift / 365.25;

                    CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, adjustedT, dfs[i], vols[i], divs[i], adjustedSpot, strikes[i]);

                    if (val == "Premium" || val == "PnL")
                    {
                        value = bb.Premium() * (cp == 0 ? strikes[i] / Spot : adjustedSpot / Spot);
                    }
                    else if (val == "Delta")
                    {
                        value = bb.Delta();
                    }
                    else if (val == "Gamma")
                    {
                        if (cp == 0)
                            value = 0;
                        else
                            value = bb.Gamma() * (cp == 0 ? strikes[i] / Spot : adjustedSpot / Spot);
                    }
                    else if (val == "Vega")
                    {
                        if (cp == 0)
                            value = 0;
                        else
                            value = bb.Vega() * (cp == 0 ? strikes[i] / Spot : adjustedSpot / Spot);
                    }
                    else if (val == "Rho")
                    {
                        value = bb.Rho(1) * (cp == 0 ? strikes[i] / Spot : adjustedSpot / Spot);
                    }
                    else if (val == "Theta")
                    {
                        if (cp == 0)
                            value = 0;
                        else
                            value = bb.Theta(1) * (cp == 0 ? strikes[i] / Spot : adjustedSpot / Spot);
                    }
                }

                values[i] = value;
            }

            return values;
        }


        [ExcelFunction(Category="ZeusXL", Description = "Black-Scholes premium as percentage of input spot")]
        public static object BSPremium(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall)
        {
            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            double premium = bb.Premium();
            return premium;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Black-Scholes delta as percentage of input spot")]
        public static object BSDelta(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall)
        {
            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            double delta = bb.Delta();
            return delta;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Black-Scholes gamma: Rate of change of delta per 1% change in spot")]
        public static object BSGamma(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall)
        {
            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            if (PutCall == 0)
                return 0;

            double gamma = bb.Gamma();
            return gamma;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Black-Scholes theta in points")]
        public static object BSTheta(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall, object DaysOpt)
        {
            double days = Utils.GetOptionalParameter(DaysOpt, 1);

            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            if (PutCall == 0)
                return 0;

            double theta = bb.Theta(days);
            return theta;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Black-Scholes rho for 100bp parallel shift")]
        public static object BSRho(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall, object DaysOpt)
        {
            double days = Utils.GetOptionalParameter(DaysOpt, 1);

            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            double rho = bb.Rho(days);
            return rho;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Black-Scholes vega as percentage of input spot")]
        public static object BSVega(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall)
        {
            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            if (PutCall == 0)
                return 0;

            double vega = bb.Vega();
            return vega;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Option VaR based on bumping spot & vol")]
        public static object OptionVaR(double Spot, double DivYield, double Vol, double T, double DF, double Strike, int PutCall,
                                       object Skew90Minus110Opt, object PercentileOpt)
        {
            double skew = Utils.GetOptionalParameter(Skew90Minus110Opt, 0.0);
            double percentile = Utils.GetOptionalParameter(PercentileOpt, 0.95);

            CallPut cp = (CallPut)PutCall;
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(cp, T, DF, Vol, DivYield, Spot, Strike);

            double nStdev = Normal.Inverse(percentile);
            double spotReturn = nStdev * PutCall * Math.Abs(Vol) * Math.Sqrt(T);
            double BumpedSpot = Spot * (1 + spotReturn);
            double BumpedVol = Vol + skew * spotReturn / 0.2;

            CommonTypes.Maths.BlackScholes bb2 = new BlackScholes(cp, T, DF, BumpedVol, DivYield, BumpedSpot, Strike);

            double VaR = bb2.Premium() - bb.Premium();

            // This doesn't work - a 1+ sigma move is too big to do an infinitesimal expansion on with just 1st/2nd order greeks!
            //double VaR2 = bb.Delta() * spotReturn + 0.5 * bb.Gamma() * (spotReturn * spotReturn) + bb.Vega() * (BumpedVol - Vol) * 100;

            return VaR;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns timeseries of EWMA volatility")]
        public static object EWMAVol(object[] Data, double DecayFactor, object FullVolSeriesOpt, object ReverseDataOpt)
        {
            bool fullSeries = Utils.GetOptionalParameter(FullVolSeriesOpt, false);
            bool reverseData = Utils.GetOptionalParameter(ReverseDataOpt, false);

            double[] data = Utils.GetVector<double>(Data);

            if (reverseData)
                data = data.Reverse().ToArray();

            EWVol ew = new EWVol(DecayFactor);

            double[,] ret = new double[data.Length, 1];
            for (int i = 0; i < ret.Length; ++i)
            {
                int index = (reverseData ? ret.Length - i - 1 : i);
                ret[index, 0] = ew.Update(data[i]);
            }

            if (fullSeries)
                return ret;
            else
                return (reverseData ? ret[0, 0] : ret[data.Length - 1, 0]);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns EMA of timeseries")]
        public static object EMA(object[] Data, double DecayFactor, object FullSeriesOpt, object ReverseDataOpt)
        {
            bool fullSeries = Utils.GetOptionalParameter(FullSeriesOpt, false);
            bool reverseData = Utils.GetOptionalParameter(ReverseDataOpt, false);

            double[] data = Utils.GetVector<double>(Data);

            if (reverseData)
                data = data.Reverse().ToArray();

            EMA em = new CommonTypes.EMA(DecayFactor);

            double[,] ret = new double[data.Length, 1];
            for (int i = 0; i < ret.Length; ++i)
            {
                int index = (reverseData ? ret.Length - i - 1 : i);
                ret[index, 0] = em.Update(data[i]);
            }

            if (fullSeries)
                return ret;
            else
                return (reverseData ? ret[0, 0] : ret[data.Length - 1, 0]);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns SMA of timeseries")]
        public static object SMA(object[] Data, int WindowLength, object FullSeriesOpt, object ReverseDataOpt)
        {
            bool fullSeries = Utils.GetOptionalParameter(FullSeriesOpt, false);
            bool reverseData = Utils.GetOptionalParameter(ReverseDataOpt, false);

            double[] data = Utils.GetVector<double>(Data);

            if (reverseData)
                data = data.Reverse().ToArray();

            SMA sm = new CommonTypes.SMA(WindowLength);

            double[,] ret = new double[data.Length, 1];
            for (int i = 0; i < ret.Length; ++i)
            {
                int index = (reverseData ? ret.Length - i - 1 : i);
                ret[index, 0] = sm.Update(data[i]);
            }

            if (fullSeries)
                return ret;
            else
                return (reverseData ? ret[0, 0] : ret[data.Length - 1, 0]);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Arrange a set of input instruments such that their loadings onto a given eigenvector are monotonic")]
        public static object RotateCorrs(double[,] Data, object[] Labels, int EigenVectorIndex)
        {
            int nRows = Data.GetLength(0), nCols = Data.GetLength(1);
            double[,] data = (double[,])Data.Clone();
            double[,] CorrelationMatrix = Accord.Statistics.Tools.Correlation(data);

            Accord.Math.Decompositions.EigenvalueDecomposition evd = new Accord.Math.Decompositions.EigenvalueDecomposition(CorrelationMatrix);
            double[,] values = evd.DiagonalMatrix;
            double[,] vectors = evd.Eigenvectors;

            double[] eigenVector = vectors.GetColumn(nCols - EigenVectorIndex - 1);
            double[] indices = Enumerable.Range(0, Labels.Length).Select(x => (double)x).ToArray();
            Array.Sort(eigenVector, indices);

            object[,] permutedData = new object[nRows+1, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (i == 0)
                        permutedData[0, j] = Labels[(int)indices[j]];

                    permutedData[i+1, j] = Data[i, (int)indices[j]];
                }
            }

            return permutedData;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Pairwise multiplication")]
        public static object MultiplyVectors(object[] Data1, object[] Data2)
        {
            double[] data1 = Utils.GetVector<double>(Data1);
            double[] data2 = Utils.GetVector<double>(Data2);

            if (data1.Length != data2.Length)
                return "Error, mismatched vectors!";

            object[] ret = new object[data2.Length];
            for (int i = 0; i < data2.Length; ++i)
            {
                ret[i] = data1[i] * data2[i];
            }

            return ret.ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Fits a polynomial to data and fills empty data with interpolated values")]
        public static object GetInterpolatedValues(object[] X, object[] Y, object OnlyReturnBlanks)
        {
            bool flag = Utils.GetOptionalParameter(OnlyReturnBlanks, false);

            SortedDictionary<double, double> xy = new SortedDictionary<double,double>();
            for (int i = 0; i < X.Length; ++i)
            {
                if (i < Y.Length && !(Y[i] is ExcelMissing || Y[i] is ExcelEmpty || Y[i] is ExcelError || Y[i] is string))
                {
                    xy.Add((double)X[i], (double)Y[i]);
                }
            }

            CubicSplineInterpolation csi = new CubicSplineInterpolation(xy.Keys.ToArray(), xy.Values.ToArray());
            double[] values = new double[X.Length];
            for (int i = 0; i < X.Length; ++i)
            {
                if (!flag || !xy.ContainsKey((double)X[i]))
                    values[i] = csi.Interpolate((double)X[i]);
            }

            return values.ToRange();
        }
    }
}
