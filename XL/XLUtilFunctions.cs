using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ExcelDna.Integration;
using Accord.Statistics.Visualizations;
using CommonTypes;
using RDotNet;


namespace XL
{
    public class XLUtilFunctions
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Creates a histogram")]
        public static object CreateHistogram(int nbins, double[] data)
        {
            Histogram hist = new Histogram();
            hist.Compute(data, nbins);

            double[,] ret = new double[nbins, 2];
            for (int i = 0; i < nbins; ++i)
            {
                ret[i, 0] = hist.Bins[i].Range.Max;
                ret[i, 1] = hist.Values[i];
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Sums the absolute values of the members of a vector")]
        public static object SumAbs(double[,] data)
        {
            double sumAbs = 0;
            foreach (double d in data)
            {
                sumAbs += Math.Abs(d);
            }

            return sumAbs;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Gets the current sheet's name")]
        public static object SheetName()
        {
            ExcelReference Caller;
            string BookAndSheetName;

            Caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            BookAndSheetName = (string)XlCall.Excel(XlCall.xlSheetNm, Caller);

            return BookAndSheetName.Split(']').Last();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Within a matrix, switch columns")]
        public static object MoveColumn(object[,] Data, int ColumnFromIndex, int ColumnToIndex)
        {
            int nRows = Data.GetLength(0), nCols = Data.GetLength(1);

            object[,] newData = new object[nRows, nCols];
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (j != ColumnToIndex)
                    {
                        if (j == ColumnFromIndex)
                        {
                            object temp = Data[i, ColumnToIndex];
                            newData[i, ColumnToIndex] = Data[i, ColumnFromIndex];
                            newData[i, ColumnFromIndex] = temp;
                        }
                        else
                        {
                            newData[i, j] = Data[i, j];
                        }
                    }
                }
            }

            return newData;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Split a string based on a set of delimiters")]
        public static object SplitString(string Data, string Delimiters)
        {
            return Data.Split(Delimiters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Concatenate several strings with a given delimiter")]
        public static object ConcatStrings(object[] Data, string Delimiter)
        {
            object[] data = Utils.GetVector<object>(Data);
            return string.Join(Delimiter, data);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Return list of unique values")]
        public static object UniqueValues(object[] Data)
        {
            object[] data = Utils.GetVector<object>(Data);

            Dictionary<object, int> unique = new Dictionary<object, int>();
            for (int i = 0; i < data.Length; ++i)
            {
                unique[data[i]] = 1;
            }

            return unique.Keys.OrderBy(x => x).ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Compresses a matrix of values into a vector and unique-ifies them")]
        public static object[,] MakeUniqueVector(object[,] Data, object ExcludeBlanksOpt)
        {
            object[,] data = Utils.GetMatrix<object>(Data);
            bool excludeBlanks = Utils.GetOptionalParameter(ExcludeBlanksOpt, false);
            Dictionary< object, int> unique = new Dictionary<object,int>();

            int nRows = data.GetLength(0), nCols = data.GetLength(1);
            for (int i = 0; i < nRows; ++i)
            {
                for (int j = 0; j < nCols; ++j)
                {
                    if (data[i,j] != null && (!excludeBlanks || (excludeBlanks && (data[i, j].ToString() != string.Empty || data[i, j].ToString() == "0"))))
                        unique[data[i, j]] = 1;
                }
            }

            int n = unique.Count, x = 0;
            object[,] ret = new object[n, 1];
            foreach (object k in unique.Keys)
            {
                ret[x, 0] = k;
                x++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Replaces a subarray")]
        public static object ReplaceSubArray(object[,] Data, int StartRow, int StartCol, object[,] NewData)
        {
            object[,] data = Utils.GetMatrix<object>(Data);
            int nRows = data.GetLength(0), nCols = data.GetLength(1);

            object[,] newData = Utils.GetMatrix<object>(NewData);
            int nRows2 = newData.GetLength(0), nCols2 = newData.GetLength(1);

            for (int i = StartRow; i < Math.Min(nRows, nRows2 + StartRow); i++)
            {
                for (int j = StartCol; j < Math.Min(nCols, nCols2 + StartCol); j++)
                {
                    data[i, j] = newData[i - StartRow, j - StartCol];
                }
            }

            return data;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Join 2 vectors")]
        public static object JoinVectors(object[] Vector1, object[] Vector2)
        {
            object[] v1 = Utils.GetVector<object>(Vector1);
            object[] v2 = Utils.GetVector<object>(Vector2);

            return v1.Concat(v2).ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Batch templating")]
        public static object BatchReplace(string Template, object[] Keys, object[] Values)
        {
            string[] keys = Utils.GetVector<string>(Keys);
            string[] values = Utils.GetVector<string>(Values);

            StringBuilder sb = new StringBuilder(Template);
            for (int i = 0; i < keys.Length; ++i)
            {
                sb.Replace(keys[i], values[i]);
            }

            string ss = sb.ToString();
            //if (keys.Where(x => ss.Contains(x)).Count() > 0)
                //return "ERROR - batch replace not complete! " + ss;

            return ss;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Given a handle name, delete the associated object from memory")]
        public static string DeleteHandle(object[] HandleNames)
        {
            int removed = 0;
            foreach (string s in HandleNames)
            {
                if (XLOM.Remove(s))
                    removed++;
            }

            return removed + " objects deleted";
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Given a handle name, display it (fingers crossed!)")]
        public static object[,] DisplayHandle(string Handle)
        {
            object o = XLOM.Get<object>(Handle);

            Type t = o.GetType();
            if (o is IEnumerable<double>)
            {
                IEnumerable<double> oo = o as IEnumerable<double>;

                object[,] data = new object[oo.Count(), 1];
                int i = 0;
                foreach (double dd in oo)
                {
                    data[i++, 0] = dd;
                }

                return data;
            }
            else if (t == typeof(double[,]))
            {
                double[,] oo = o as double[,];
                int nRows = oo.GetLength(0), nCols = oo.GetLength(1);

                object[,] data = new object[nRows, nCols];
                for (int i = 0; i < nRows; ++i)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        data[i, j] = oo[i, j];
                    }
                }

                return data;
            }
            else if (t == typeof(object[,]))
            {
                return (o as object[,]);
            }
            else if (o is IEnumerable<ITimestampedDatum>)
            {
                IEnumerable<ITimestampedDatum> oo = o as IEnumerable<ITimestampedDatum>;

                object[,] data = new object[oo.Count(), 1];
                int i = 0;
                foreach (ITimestampedDatum dd in oo)
                {
                    data[i++, 0] = dd.ToString();
                }

                return data;
            }

            throw new Exception("Error, unrecognised handle type in DisplayHandle()!");
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Create key-value store in memory")]
        public static object CreateKeyValueMap(object[] Keys, object[] Values, object HandleNameOpt)
        {
            string handleName = Utils.GetOptionalParameter(HandleNameOpt, "KeyValueMap");

            string[] keys = Utils.GetVector<string>(Keys);
            object[] values = Utils.GetVector<object>(Values);

            Dictionary<string, object> map = new Dictionary<string, object>();

            for (int i = 0; i < keys.Length; ++i)
            {
                if (XLOM.Contains(values[i].ToString(), true))
                    map.Add(keys[i], XLOM.Get(values[i].ToString()));
                else
                    map.Add(keys[i], values[i]);
            }

            return XLOM.Add(handleName, map);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Descriptive statistics of the returns of a price series")]
        public static object DescribeReturns(object[,] PriceSeries, object HideLabelsOpt, object AnnualisationFactorOpt,
                             object AnnualRiskFreeRateOpt, object UseBPReturnsOpt, object ExcludeZeroReturnsOpt)
        {
            double[,] priceSeries = Utils.GetMatrix<double>(PriceSeries);
            bool hideLabels = Utils.GetOptionalParameter(HideLabelsOpt, false);
            double annFactor = Utils.GetOptionalParameter(AnnualisationFactorOpt, 1.0);
            double riskFreeRate = Utils.GetOptionalParameter(AnnualRiskFreeRateOpt, 0.0);
            bool useBPReturns = Utils.GetOptionalParameter(UseBPReturnsOpt, false);
            bool excludeZeroes = Utils.GetOptionalParameter(ExcludeZeroReturnsOpt, false);

            if (priceSeries.Length == 0)
                return "No data!";

            int nAssets = priceSeries.GetLength(1), nDates = priceSeries.GetLength(0);
            object[,] ret = new object[10, nAssets + (hideLabels ? 0 : 1)];
            for (int a = 0; a < nAssets; ++a)
            {
                double maxLevel = priceSeries[0, a], maxDD = 1e10;
                SummaryContainer sc = new SummaryContainer();
                DistributionContainer dc = new DistributionContainer(1e-7);
                for (int i = 1; i < nDates; ++i)
                {
                    double r = (useBPReturns ? (priceSeries[i, a] - priceSeries[i - 1, a]) / 100 : priceSeries[i, a] / priceSeries[i - 1, a] - 1);
                    if (!excludeZeroes || r != 0)
                    {
                        sc.Add(r);
                        dc.Add(r);
                    }

                    if (priceSeries[i, a] > maxLevel)
                        maxLevel = priceSeries[i, a];

                    double dd = (useBPReturns ? (priceSeries[i, a] - maxLevel) / 100 : priceSeries[i, a] / maxLevel - 1);
                    if (dd < maxDD)
                        maxDD = dd;
                }

                int dataIndex = a + (hideLabels ? 0 : 1);
                ret[0, dataIndex] = sc.Average * annFactor;
                ret[1, dataIndex] = sc.Stdev * Math.Sqrt(annFactor);
                ret[2, dataIndex] = (ret[0, dataIndex].AsDouble() - riskFreeRate) / ret[1, dataIndex].AsDouble();
                ret[3, dataIndex] = maxLevel / priceSeries[0, a];
                ret[4, dataIndex] = maxDD;
                ret[5, dataIndex] = -maxDD / (sc.Stdev * Math.Sqrt(annFactor));
                ret[6, dataIndex] = (sc.Average * annFactor) / -maxDD;
                ret[7, dataIndex] = priceSeries[nDates - 1, a] / priceSeries[0, a];
                ret[8, dataIndex] = dc.GetValue(0.05);
                ret[9, dataIndex] = dc.GetValue(0.01);
            }

            if (!hideLabels)
            {
                ret[0, 0] = "Average Return";
                ret[1, 0] = "Volatility";
                ret[2, 0] = "Sharpe";
                ret[3, 0] = "High Watermark";
                ret[4, 0] = "Max Drawdown";
                ret[5, 0] = "Max DD / Vol";
                ret[6, 0] = "Avg Ret / Max DD";
                ret[7, 0] = "Final Level";
                ret[8, 0] = "VaR95";
                ret[9, 0] = "VaR99";
            }

            return ret;
        }
    }


    public class MyAddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            // Install the synchronization helper.
            ExcelAsyncUtil.Initialize();
        }

        public void AutoClose()
        {
            // Probably not useful ...
            ExcelAsyncUtil.Uninitialize();       
        }
    }
    
	public static class ResizeTestFunctions
	{
        public static object MakeArray(int rows, int columns)
        {
            object[,] result = new string[rows, columns];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    result[i,j] = string.Format("({0},{1})", i, j);
                }
            }
            
            return result;
        }

        public static object MakeArrayAndResize(int rows, int columns)
        {
            object result = MakeArray(rows, columns);            
            // Call Resize via Excel - so if the Resize add-in is not part of this code, it should still work.
            return XlCall.Excel(XlCall.xlUDF, "Resize", result);
        }
    }

    public class ArrayResizer
    {
        static HashSet<ExcelReference> ResizeJobs = new HashSet<ExcelReference>();

        // This function will run in the UDF context.
        // Needs extra protection to allow multithreaded use.
        [ExcelFunction(Category = "ZeusXL", Description = "Resize an Excel array function.", IsVolatile = false, IsMacroType = false)]
        public static object Resize(object[,] array)
        {
            ExcelReference caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
            if (caller == null) return array;

            int rows = array.GetLength(0);
            int columns = array.GetLength(1);

            if ((caller.RowLast - caller.RowFirst + 1 != rows) ||
                (caller.ColumnLast - caller.ColumnFirst + 1 != columns))
            {
                // Size problem: enqueue job, call async update and return #N/A
                // TODO: Add guard for ever-changing result?
                EnqueueResize(caller, rows, columns);
                ExcelAsyncUtil.QueueAsMacro(DoResizing);
                return ExcelError.ExcelErrorNA;
            }

            // Size is already OK - just return result
            return array;
        }

        static void EnqueueResize(ExcelReference caller, int rows, int columns)
        {
            ExcelReference target = new ExcelReference(caller.RowFirst, caller.RowFirst + rows - 1, caller.ColumnFirst, caller.ColumnFirst + columns - 1, caller.SheetId);

            try
            {
                ResizeJobs.Add(target);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //if (ResizeJobs.Count < 100)
            //{
            //    ResizeJobs.Enqueue(target);
            //}
        }

        static void DoResizing()
        {
            while (ResizeJobs.Count > 0)
            {
                ExcelReference target = ResizeJobs.First();
                DoResize(target);

                ResizeJobs.Remove(target);

                //DoResize(Resize.DeQueue());
            }
        }

        static void DoResize(ExcelReference target)
        {
            try
            {
                XlCall.Excel(XlCall.xlcEcho, false);

                // Get the formula in the first cell of the target
                string formula = (string)XlCall.Excel(XlCall.xlfGetCell, 41, target);
                ExcelReference firstCell = new ExcelReference(target.RowFirst, target.RowFirst, target.ColumnFirst, target.ColumnFirst, target.SheetId);

                bool isFormulaArray = (bool)XlCall.Excel(XlCall.xlfGetCell, 49, target);
                if (isFormulaArray)
                {
                    object oldSelectionOnActiveSheet = XlCall.Excel(XlCall.xlfSelection);
                    object oldActiveCell = XlCall.Excel(XlCall.xlfActiveCell);

                    // Remember old selection and select the first cell of the target
                    string firstCellSheet = (string)XlCall.Excel(XlCall.xlSheetNm, firstCell);
                    XlCall.Excel(XlCall.xlcWorkbookSelect, new object[] { firstCellSheet });
                    object oldSelectionOnArraySheet = XlCall.Excel(XlCall.xlfSelection);
                    XlCall.Excel(XlCall.xlcFormulaGoto, firstCell);

                    // Extend the selection to the whole array and clear
                    XlCall.Excel(XlCall.xlcSelectSpecial, 6);
                    ExcelReference oldArray = (ExcelReference)XlCall.Excel(XlCall.xlfSelection);

                    oldArray.SetValue(ExcelEmpty.Value);
                    XlCall.Excel(XlCall.xlcSelect, oldSelectionOnArraySheet);
                    XlCall.Excel(XlCall.xlcFormulaGoto, oldSelectionOnActiveSheet);
                }
                // Get the formula and convert to R1C1 mode
                bool isR1C1Mode = (bool)XlCall.Excel(XlCall.xlfGetWorkspace, 4);
                string formulaR1C1 = formula;
                if (!isR1C1Mode)
                {
                    // Set the formula into the whole target
                    formulaR1C1 = (string)XlCall.Excel(XlCall.xlfFormulaConvert, formula, true, false, ExcelMissing.Value, firstCell);
                }
                // Must be R1C1-style references
                object ignoredResult;
                XlCall.XlReturn retval = XlCall.TryExcel(XlCall.xlcFormulaArray, out ignoredResult, formulaR1C1, target);
                if (retval != XlCall.XlReturn.XlReturnSuccess)
                {
                    // TODO: Consider what to do now!?
                    // Might have failed due to array in the way.
                    firstCell.SetValue("'" + formula);
                }
            }
            catch (Exception ex)
            {
                XlCall.Excel(XlCall.xlcAlert, ex.ToString(), true);
            }
            finally
            {
                XlCall.Excel(XlCall.xlcEcho, true);
            }
        }
    }
}
