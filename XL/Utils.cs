using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Net;

using ExcelDna.Integration;
using CommonTypes;

namespace XL
{
    public static class Utils
    {
        public static SerialisableDictionary CreateConfigDictionary(object[,] ConfigMatrix)
        {
            SerialisableDictionary cd = new SerialisableDictionary();

            int r = ConfigMatrix.GetLength(0);
            int c = ConfigMatrix.GetLength(1);

            if (c == 2)
            {
                for (int x = 0; x < r; ++x)
                {
                    if (!ConfigMatrix[x, 0].Equals(ExcelDna.Integration.ExcelEmpty.Value))
                    {
                        if (XLOM.Contains(ConfigMatrix[x, 1].ToString(), true))
                            cd[ConfigMatrix[x, 0].ToString()] = XLOM.Get(ConfigMatrix[x, 1].ToString());
                        else
                            cd[ConfigMatrix[x, 0].ToString()] = ConfigMatrix[x, 1];
                    }
                }
            }
            else if (c > 2)
            {
                for (int cc = 1; cc < c; ++cc)
                {
                    string colName = ConfigMatrix[0, cc].ToString();
                    Dictionary<string, object> cdd = new Dictionary<string, object>();
                    for (int x = 1; x < r; ++x)
                    {
                        cdd[ConfigMatrix[x, 0].ToString()] = ConfigMatrix[x, cc];
                    }

                    cd.Add(colName, cdd);
                }
            }

            return cd;
        }


        public static object GetColumnData(this Dictionary<string, object> dict, string colName, string key)
        {
            return (dict[colName] as Dictionary<string, object>)[key];
        }


        static double AsDouble(this object doubleValue)
        {
            return (double)doubleValue;
        }


        static int AsInt(this object intValue)
        {
            return (int)intValue.AsDouble();
        }


        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i]))
                    return false;
            }

            return true;
        }


        public static bool StartExternalProcess(string path, params string[] args)
        {
            // Prepare the process to run
            ProcessStartInfo start = new ProcessStartInfo();
            
            // Enter in the command line arguments, everything you would enter after the executable name itself
            start.Arguments = string.Join(" ", args);

            // Enter the executable to run, including the complete path
            start.FileName = path;

            // Do you want to show a console window?
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = true;
            
            // Run the external process & wait for it to finish
            Process proc = Process.Start(start);

            return false;
        }


        public static T GetOptionalParameter<T>(object var, T defaultValue)
        {
            if (!(var is ExcelMissing) && !(var is ExcelEmpty) && !(var is ExcelError))
            {
                // Small hack.
                if (typeof(T) == typeof(int) && var.GetType() == typeof(double))
                    return (T)Convert.ChangeType((double)var, typeof(T));
                else
                    return (T)var;
            }
            else
            {
                return defaultValue;
            }
        }


        public static T[] GetVector<T>(object[] data)
        {
            List<T> ret = new List<T>();
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] is ExcelMissing || data[i] is ExcelEmpty || data[i] is ExcelError)
                    break;

                ret.Add((T)data[i]);
            }

            return ret.ToArray();
        }


        public static T[,] GetMatrix<T>(object[,] data)
        {
            int nRowsMax = data.GetLength(0), nColsMax = data.GetLength(1);
            int nRows = -1, nCols = -1;

            for (int i = nRowsMax - 1; i >= 0; --i)
            {
                for (int j = nColsMax - 1; j >= 0; --j)
                {
                    if (data[i, j] is ExcelMissing || data[i, j] is ExcelEmpty || data[i, j] is ExcelError)
                        continue;
                    else
                    {
                        nCols = Math.Max(nCols, j + 1);
                        break;
                    }
                }
            }

            for (int j = 0; j < nCols; ++j)
            {
                for (int i = nRowsMax - 1; i >= 0; --i)
                {
                    if (data[i, j] is ExcelMissing || data[i, j] is ExcelEmpty || data[i, j] is ExcelError)
                        continue;
                    else
                    {
                        nRows = Math.Max(nRows, i + 1);
                        break;
                    }
                }
            }

            T[,] ret = new T[nRows, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (data[i, j] is ExcelMissing || data[i, j] is ExcelEmpty || data[i, j] is ExcelError)
                        ret[i, j] = default(T);
                    else
                        ret[i, j] = (T)data[i, j];
                }
            }

            return ret;
        }


        [ExcelFunction(IsHidden = true)]
        public static DateTime GetDate(object Date, DateTime defaultValue)
        {
            DateTime temp;
            if (Date is ExcelMissing || Date is ExcelEmpty || Date is ExcelError)
            {
                temp = defaultValue;
            }
            else if (!DateTime.TryParse(Date.ToString(), out temp))
            {
                temp = DateTime.FromOADate(double.Parse(Date.ToString()));
            }

            return temp;
        }


        public static object[,] ToRange<T>(this IEnumerable<T> Data)
        {
            int n = Data.Count();
            object[,] ret = new object[n, 1];

            int i = 0;
            foreach (T d in Data)
            {
                ret[i++, 0] = d;
            }

            return ret;
        }
    }

    public static class BasicAnalytics
    {
        [ExcelFunction(IsHidden = true)]
        public static double[] CalculateReturns(double[] data, int numDaysReturns, bool useBPReturns)
        {
            double[] ret = new double[data.Length];
            for (int i = numDaysReturns; i < data.Length; ++i)
            {
                ret[i] = (useBPReturns ? (data[i] - data[i - numDaysReturns]) / 100 : (data[i] / data[i - numDaysReturns] - 1));
            }

            return ret;
        }


        // This returns the version of the Sharpe with the compounding to match CM2.
        [ExcelFunction(IsHidden = true)]
        public static double[] GetSharpe(double[] prices, int numDaysReturns, bool useBPReturns, int SharpeWindow,
                                         int annualisationFactor, bool returnZScore, int ZWindow, bool exponentialSharpe)
        {
            double[] ret = CalculateReturns(prices, numDaysReturns, useBPReturns);
            int nRet = prices.Length;

            CircularBuffer<double> returns = new CircularBuffer<double>(SharpeWindow);
            CircularBuffer<double> sharpes = new CircularBuffer<double>(ZWindow);

            double decayFactor = 1 - 2.0 / (SharpeWindow + 1);
            EMA r_ema = new EMA(decayFactor);
            EWVol v_ema = new EWVol(decayFactor);

            for (int i = 0; i < nRet; ++i)
            {
                returns.Insert(ret[i]);

                if (returns.Full)
                {
                    double sharpe = 0;
                    if (!exponentialSharpe)
                    {
                        double r_t = (prices[i] - prices[i - SharpeWindow + 1]) / prices[i - SharpeWindow + 1] / SharpeWindow;

                        sharpe = r_t / returns.SD() * Math.Sqrt(annualisationFactor);
                    }
                    else
                    {
                        r_ema.Update(ret[i]);
                        v_ema.Update(ret[i]);

                        sharpe = r_ema.Value / v_ema.Value * Math.Sqrt(annualisationFactor);
                    }

                    sharpes.Insert(sharpe);
                }
            }

            double[] r = new double[2];
            r[0] = sharpes.Last();
            r[1] = sharpes.Last() / sharpes.SD();

            return r;
        }
    }
}
