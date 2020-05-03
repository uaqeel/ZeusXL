using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Data;
using System.Text.RegularExpressions;

using ExcelDna.Integration;
using Bloomberglp.Blpapi;
using Accord.Statistics;
using Accord.Math;
using CommonTypes;
using Strategies;


namespace XL
{
    public partial class XLDataFunctions
    {
         [ExcelFunction(Category = "ZeusXL", Description = "Create a data source from spreadsheet data")]
        public static object CreateMarketDataSource(string ContractHandle, object[] Timestamps, object[] MidPrices, object SpreadOpt)
        {
            double spread = Utils.GetOptionalParameter(SpreadOpt, 0.0);

            Contract cc = XLOM.Get<Contract>(ContractHandle);

            object[] timestamps = Utils.GetVector<object>(Timestamps);
            double[] midprices = Utils.GetVector<double>(MidPrices);

            int nData = timestamps.Length;
            bool oaDate = (timestamps[0].GetType() == typeof(double));

            List<ITimestampedDatum> DataSource = new List<ITimestampedDatum>(nData);
            for (int i = 0; i < nData; ++i)
            {
                DateTimeOffset now = new DateTimeOffset((oaDate ? DateTime.FromOADate((double)timestamps[i]) : DateTime.Parse(timestamps[i].ToString())), new TimeSpan(0, 0, 0));
                DataSource.Add(new Market(now, cc.Id, (decimal)(midprices[i] * (1 - 0.5 * spread)), (decimal)(midprices[i] * (1 + 0.5 * spread))));
            }

            DataSourceInfo dsi = new DataSourceInfo("InMemoryMarketDataSource", cc.Id, cc.Symbol);

            if (XLOM.Contains(dsi.ToString()))
                XLOM.Remove(dsi.ToString());

            return XLOM.Add(dsi.ToString(), DataSource);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Create a deposit rate data source from spreadsheet data")]
        public static object CreateDepositRateDataSource(string Currency, object[] Timestamps, double[] DepositRates)
        {
            int nData = Timestamps.Length;
            bool oaDate = (Timestamps[0].GetType() == typeof(double));

            List<ITimestampedDatum> DataSource = new List<ITimestampedDatum>(nData);
            for (int i = 0; i < nData; ++i)
            {
                DateTimeOffset now = new DateTimeOffset((oaDate ? DateTime.FromOADate((double)Timestamps[i]) :
                                                                  DateTime.Parse(Timestamps[i].ToString())), new TimeSpan(0, 0, 0));

                DataSource.Add(new DepositRate(now, Currency, DepositRates[i]));
            }

            DataSourceInfo dsi = new DataSourceInfo("InMemoryDepositRateDataSource", 0, Currency);

            if (XLOM.Contains(dsi.ToString()))
                XLOM.Remove(dsi.ToString());

            return XLOM.Add(dsi.ToString(), DataSource);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Create an implied vol data source from spreadsheet data")]
        public static object CreateImpliedVolDataSource(int ContractId, object[] Timestamps, double[] ImpliedVols)
        {
            int nData = Timestamps.Length;
            bool oaDate = (Timestamps[0].GetType() == typeof(double));

            List<ITimestampedDatum> DataSource = new List<ITimestampedDatum>(nData);
            for (int i = 0; i < nData; ++i)
            {
                DateTimeOffset now = new DateTimeOffset((oaDate ? DateTime.FromOADate((double)Timestamps[i]) :
                                                                  DateTime.Parse(Timestamps[i].ToString())), new TimeSpan(0, 0, 0));

                DataSource.Add(new ImpliedVol(now, ContractId, ImpliedVols[i]));
            }

            DataSourceInfo dsi = new DataSourceInfo("InMemoryImpliedVolDataSource", ContractId, "ATMF");

            if (XLOM.Contains(dsi.ToString()))
                XLOM.Remove(dsi.ToString());

            return XLOM.Add(dsi.ToString(), DataSource);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Load XML fixing schedule (Maestro format)")]
        public static object LoadFixings(string ticker, object BaseURLOpt, object ForceReloadOpt)
        {
            string baseURl = Utils.GetOptionalParameter(BaseURLOpt, @"http://cg028486/jupiter/data/fixings");
            bool forceReloadOpt = Utils.GetOptionalParameter(ForceReloadOpt, false);

            string key = ticker + "_fixings";
            SortedDictionary<DateTime, Tuple<double, double, double, double, string>> fixings = XLOM.Get<SortedDictionary<DateTime, Tuple<double, double, double, double, string>>>(key);
            if (forceReloadOpt || fixings == null)
            {
                fixings = new SortedDictionary<DateTime, Tuple<double, double, double, double, string>>();

                // Eg, http://cg028486/jupiter/data/fixings/ES1.xml
                string url = string.Format(@"{0}/{1}.xml", baseURl, ticker);

                XmlDocument xml = new XmlDocument();
                xml.Load(new XmlTextReader(url));

                var fixingNodes = xml.DocumentElement.SelectNodes("FixingSchedules/FixingSchedule/Fixings/EqFixing");
                foreach (XmlNode fixingNode in fixingNodes)
                {
                    DateTime date = DateTime.Parse(fixingNode["Date"].InnerText);
                    string comment = fixingNode["Comments"].InnerText;

                    double open = -1;
                    double.TryParse(fixingNode["Open"].InnerText, out open);

                    double low = -1;
                    double.TryParse(fixingNode["Low"].InnerText, out low);

                    double high = -1;
                    double.TryParse(fixingNode["High"].InnerText, out high);

                    double close = -1;
                    double.TryParse(fixingNode["Close"].InnerText, out close);

                    if (close != 0)
                        fixings.Add(date, new Tuple<double, double, double, double, string>(open, low, high, close, comment));
                }

                XLOM.Add(ticker + "_fixings", fixings);
            }

            object[,] ret = new object[fixings.Count + 1, 6];
            ret[0, 0] = "Date";
            ret[0, 1] = "Open";
            ret[0, 2] = "Low";
            ret[0, 3] = "High";
            ret[0, 4] = "Close";
            ret[0, 5] = "Comment";

            int i = 1;
            foreach (var fixing in fixings) {
                ret[i, 0] = fixing.Key;
                ret[i, 1] = fixing.Value.Item1;
                ret[i, 2] = fixing.Value.Item2;
                ret[i, 3] = fixing.Value.Item3;
                ret[i, 4] = fixing.Value.Item4;
                ret[i, 5] = fixing.Value.Item5;

                i++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Returns the subset of Data for which SelectFlags is true")]
        public static object SelectWhereTrue(object[,] Data, object[] SelectFlags)
        {
            int nData = SelectFlags.Length;
            if (Data.GetLength(0) != nData)
                throw new Exception("Error, inconsistent data lengths!");

            int nTrue = SelectFlags.Where(y => !(y is ExcelError) && (bool)y == true).Count();
            int nCols = Data.GetLength(1);

            object[,] ret = new object[nTrue, nCols];
            int x = 0;
            for (int i = 0; i < nData; ++i)
            {
                if (!(SelectFlags[i] is ExcelError) && (bool)SelectFlags[i])
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        ret[x, j] = Data[i, j];
                    }

                    x++;
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Run SQL SELECT expression on data")]
        public static object SQLEval(object[,] Data, string SelectExpression, object SortExpressionOpt, object[] ColumnsToSelectOpt, object HideColumnNamesOpt)
        {
            string sort = Utils.GetOptionalParameter(SortExpressionOpt, string.Empty);
            bool hideColumnNames = Utils.GetOptionalParameter(HideColumnNamesOpt, false);
            object[,] data = Utils.GetMatrix<object>(Data);
            string[] columnsToSelect = Utils.GetVector<string>(ColumnsToSelectOpt);

            int nRows = data.GetLength(0), nCols = data.GetLength(1);

            DataTable dt = new DataTable();
            for (int i = 0; i < nCols; ++i)
            {
                if (data[0, i] != null)
                {
                    Type t = typeof(double);
                    if (data[1,i] != null)
                        t = (data[1, i].GetType() == typeof(string) ? typeof(string) : typeof(double));

                    dt.Columns.Add(data[0, i].ToString(), t);
                }
                else
                {
                    dt.Columns.Add(i.ToString(), typeof(double));
                }
            }

            for (int r = 1; r < nRows; ++r)
            {
                object[] row = new object[nCols];
                for (int c = 0; c < nCols; ++c)
                {
                    if (data[r, c] == null || data[r, c].ToString() == string.Empty || data[r, c].GetType() != dt.Columns[c].DataType)
                        row[c] = 0;
                    else
                        row[c] = data[r, c];
                }

                dt.Rows.Add(row);
            }

            // Run Select query.
            DataRow[] dr = null;
            try
            {
                if (sort == string.Empty)
                {
                    dr = dt.Select(SelectExpression);
                }
                else
                {
                    dr = dt.Select(SelectExpression, sort);
                }

                nRows = dr.Length;
            }
            catch (Exception e)
            {
                return "Error running SQL: " + e.ToString();
            }

            // Select columns to show.
            if (columnsToSelect.Length != 0 && nRows > 0)
            {
                dt = dr.CopyToDataTable();

                DataView dv = new DataView(dt);
                dt = dv.ToTable(false, columnsToSelect);
                dr = dt.Select();

                nCols = dt.Columns.Count;
            }

            if (nRows == 0)
                return "No matching rows!";

            object[,] ret = new object[nRows + (hideColumnNames ? 0 : 1), nCols];
            for (int r = 0; r < nRows; ++r)
            {
                for (int c = 0; c < nCols; ++c)
                {
                    if (!hideColumnNames && r == 0)
                        ret[r, c] = dt.Columns[c].ColumnName;

                    ret[r + (hideColumnNames ? 0 : 1), c] = dr[r][c];
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Reverse a range")]
        public static object Reverse(object[] Data)
        {
            object[] data = Utils.GetVector<object>(Data);

            return data.Reverse().ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Calculate returns for a vector of prices")]
        public static object GetReturns(object[,] Data, object IsDataReversedOpt, object UseBPReturnsOpt, object NumDaysReturnsOpt)
        {
            double[,] data = Utils.GetMatrix<double>(Data);
            bool reverse = Utils.GetOptionalParameter(IsDataReversedOpt, false);
            bool useBPReturns = Utils.GetOptionalParameter(UseBPReturnsOpt, false);
            int numDaysReturns = Utils.GetOptionalParameter(NumDaysReturnsOpt, 1);

            int nAssets = data.GetLength(1), nDates = data.GetLength(0);
            double[,] ret = new double[nDates, nAssets];
            for (int i = 0; i < nAssets; ++i)
            {
                double[] temp = BasicAnalytics.CalculateReturns((reverse ? data.Column(i).Reverse().ToArray() : data.Column(i)), numDaysReturns, useBPReturns);
                if (reverse)
                    temp = temp.Reverse().ToArray();

                for (int j = 0; j < nDates; ++j)
                {
                    ret[j, i] = temp[j];
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Calculate portfolio vol from vector of weights and series of price levels")]
        public static object CalculatePortfolioVol(object[] Weights, object[,] PriceLevels,
                                                   object IsDataReversedOpt, object NumDaysReturnsOpt, object AnnualisationFactorOpt)
        {
            double[] weights = Utils.GetVector<double>(Weights);
            double[,] prices = Utils.GetMatrix<double>(PriceLevels);
            bool reverse = Utils.GetOptionalParameter(IsDataReversedOpt, false);
            int numDaysReturns = Utils.GetOptionalParameter(NumDaysReturnsOpt, 1);
            int annualFactor = Utils.GetOptionalParameter(AnnualisationFactorOpt, 252);

            int nAssets = weights.Length;
            int nDates = prices.GetLength(0);

            if (reverse)
            {
                double[,] temp = new double[nDates, nAssets];
                for (int i = 0; i < nAssets; ++i)
                {
                    var rev = prices.Column(i).Reverse().ToArray();
                    for (int j = 0; j < nDates; ++j)
                    {
                        temp[j, i] = rev[j];
                    }
                }

                prices = temp;
            }

            double[,] returns = new double[nDates, nAssets];
            for (int i = 0; i < nAssets; ++i)
            {
                double[] ret = BasicAnalytics.CalculateReturns(prices.Column(i), numDaysReturns, false);
                for (int j = 0; j < nDates; ++j)
                {
                    returns[j, i] = ret[j];
                }
            }

            double[,] covar = returns.Covariance();

            double portfolioVol = Math.Sqrt(weights.Multiply(covar).InnerProduct(weights) * annualFactor / numDaysReturns);
            return portfolioVol;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Calculate portfolio vol from vector of position weights, asset vols and correlation matrix")]
        public static object CalculatePortfolioVol2(object[] PositionWeights, object[] AssetVols, object[,] CorrelationMatrix, object AnnualisationFactorOpt)
        {
            double[] positionWeights = Utils.GetVector<double>(PositionWeights);
            double[] assetVols = Utils.GetVector<double>(AssetVols);
            double[,] correlationMatrix = Utils.GetMatrix<double>(CorrelationMatrix);

            int annualFactor = Utils.GetOptionalParameter(AnnualisationFactorOpt, 1);

            double[] positionVols = new double[positionWeights.Length];
            for (int i = 0; i < positionWeights.Length; ++i)
            {
                positionVols[i] = positionWeights[i] * assetVols[i];
            }

            double portfolioVol = Math.Sqrt(positionVols.Multiply(correlationMatrix).InnerProduct(positionVols) * annualFactor);
            return portfolioVol;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Calculate Sharpe ratio from price series")]
        public static object Sharpe(object[] PriceSeries, int SharpeWindow, object DataIsReversedOpt,
                                    object NumDaysReturnsOpt, object UseBPReturnsOpt, object AnnualisationFactorOpt,
                                    object ReturnZScoreOpt, object ZWindowOpt, object ExponentialSharpeOpt)
        {
            double[] prices = Utils.GetVector<double>(PriceSeries);
            bool dataIsReversed = Utils.GetOptionalParameter(DataIsReversedOpt, false);
            if (dataIsReversed)
                prices = prices.Reverse().ToArray();

            int numDaysReturns = Utils.GetOptionalParameter(NumDaysReturnsOpt, 1);
            bool useBPReturns = Utils.GetOptionalParameter(UseBPReturnsOpt, false);
            int annualisationFactor = Utils.GetOptionalParameter(AnnualisationFactorOpt, 1);
            bool returnZScore = Utils.GetOptionalParameter(ReturnZScoreOpt, false);
            int ZWindow = Utils.GetOptionalParameter(ZWindowOpt, 252);
            bool exponentialSharpe = Utils.GetOptionalParameter(ExponentialSharpeOpt, false);

            double[] r = BasicAnalytics.GetSharpe(prices, numDaysReturns, useBPReturns, SharpeWindow, annualisationFactor,
                                                  returnZScore, ZWindow, exponentialSharpe);
            return r.ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Calculate VaR-95 and VaR-99 from price series")]
        public static object ValueAtRisk(object[] PriceSeries, object DataIsReversedOpt)
        {
            object[] prices = Utils.GetVector<object>(PriceSeries);
            bool dataIsReversed = Utils.GetOptionalParameter(DataIsReversedOpt, false);
            if (dataIsReversed)
                prices = prices.Reverse().ToArray();

            object[,] prices2 = new object[prices.Length, 1];
            for (int i = 0; i < prices.Length; ++i)
            {
                prices2[i, 0] = prices[i];
            }

            object[,] data = XLUtilFunctions.DescribeReturns(prices2, ExcelMissing.Value, 252.0, ExcelMissing.Value, ExcelMissing.Value, ExcelMissing.Value) as object[,];

            double[,] ret = new double[2, 1];
            int nRows = data.GetLength(0);
            for (int i = 0; i < nRows; ++i)
            {
                if (data[i, 0].ToString() == "VaR95")
                    ret[0, 0] = (double)data[i, 1];

                if (data[i, 0].ToString() == "VaR99")
                    ret[1, 0] = (double)data[i, 1];
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Combine price series with defined weights - if omitted, weights are assumed to be 100%")]
        public static object GetCombinedPriceSeries(object[,] PriceSeries, object[] NAVWeightsOpt, object IsDataReversedOpt)
        {
            bool reversed = Utils.GetOptionalParameter(IsDataReversedOpt, false);
            double[,] data = Utils.GetMatrix<double>(PriceSeries);
            double[] weights;

            int nAssets = data.GetLength(1) - 0;
            if (NAVWeightsOpt == null || NAVWeightsOpt[0] is ExcelMissing || NAVWeightsOpt[0] is ExcelEmpty)
            {
                weights = new double[nAssets];
                for (int i = 0; i < weights.Length; ++i) weights[i] = 1;
            }
            else
            {
                weights = Utils.GetVector<double>(NAVWeightsOpt);
            }
            
            if (weights.Length == 1)
            {
                double weight = weights[0];

                weights = new double[data.GetLength(1)];
                for (int i = 0; i < weights.Length; ++i)
                    weights[i] = weight;
            }

            int nDates = data.GetLength(0);
            double[] combinedData = new double[nDates];
            for (int i = 0; i < nAssets; ++i)
            {
                double[] temp = BasicAnalytics.CalculateReturns((reversed ? data.Column(i).Reverse().ToArray() : data.Column(i)), 1, false);

                for (int j = 0; j < nDates; ++j)
                {
                    combinedData[j] += weights[i] * temp[j];
                }
            }

            double[] combinedPrices = new double[nDates];
            combinedPrices[0] = 1;
            for (int i = 1; i < nDates; ++i)
            {
                combinedPrices[i] = combinedPrices[i - 1] * (1 + combinedData[i]);
            }

            if (reversed) combinedPrices = combinedPrices.Reverse().ToArray();
            return combinedPrices.ToRange();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Compare two data sources")]
        public static object LagAlign(object[] IndependentSeries, object[] DependentSeries)
        {
            double[] x = Utils.GetVector<double>(IndependentSeries);
            double[] y = Utils.GetVector<double>(DependentSeries);

            if (y.Length >= x.Length)
            {
                string errorMsg = "Error, dependent series must be shorter than independent series! (" + y.Length + " vs " + x.Length + ")";
                Debug.WriteLine(errorMsg);

                return errorMsg;
            }

            double minDistance = 1000000000;
            int minIndex = 0;
            for (int i = 0; i < x.Length - y.Length; ++i)
            {
                double distance = 0;
                for (int j = 0; j < y.Length; ++j)
                {
                    distance += Math.Pow((y[j] - x[i + j]), 2);
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minIndex = i;
                }
            }

            object[,] ret = new object[x.Length, 2];
            for (int i = 0; i < x.Length; ++i)
            {
                ret[i, 0] = x[i];
                if (i >= minIndex && (i - minIndex) < y.Length)
                {
                    ret[i, 1] = y[i - minIndex];
                }
                else
                {
                    ret[i, 1] = ExcelEmpty.Value;
                }
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Add a time interval to a date")]
        public static object DateAdd(object Date, string Interval)
        {
            DateTime date = Utils.GetDate(Date, DateTime.Now);

            Regex regex = new Regex(@"([+-]?\d?\.?\d+)(\w)");
            MatchCollection mc = regex.Matches(Interval, 0);

            double n = double.Parse(mc[0].Groups[1].Value);
            string tp = mc[0].Groups[2].Value.ToLower();

            switch (tp)
            {
                case "d": return date.AddDays(n);
                case "m": return date.AddMonths((int)n);
                case "y": return date.AddYears((int)n);
                default: return "Error: didn't recognise time interval!";
            }
        }
    }
}
