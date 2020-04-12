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
using CommonTypes;
using RDotNet;


namespace XL
{
    public partial class XLBloombergFunctions
    {
        static Session session;


        [ExcelFunction(Category = "ZeusXL", Description = "Get historical data from Bloomberg - by default retrieves total return index")]
        public static object ZDH(object[] Tickers, string StartDate, object EndDateOpt, object FieldOpt, object PeriodicityOpt,
                                 object ReverseOpt, object NoLabelsOpt, object[] NamesOpt, object NormaliseOpt)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return "#N/A";

            string[] tickers = Utils.GetVector<string>(Tickers);
            string periodicity = Utils.GetOptionalParameter(PeriodicityOpt, "Daily").ToUpper();
            string field = Utils.GetOptionalParameter(FieldOpt, "PX_LAST");
            if (field.ToLower().StartsWith("tr"))
                field = "TOT_RETURN_INDEX_NET_DVDS";

            string startDate = Utils.GetDate(StartDate, DateTime.Now).ToString("yyyyMMdd");
            string endDate = Utils.GetDate(EndDateOpt, DateTime.Now).ToString("yyyyMMdd");

            bool reverse = Utils.GetOptionalParameter(ReverseOpt, false);
            bool noLabels = Utils.GetOptionalParameter(NoLabelsOpt, false);
            string[] names = Utils.GetVector<string>(NamesOpt);
            bool normalise = Utils.GetOptionalParameter(NormaliseOpt, false);

            // Retrieve required data.
            string[] tickersToRetrieve = tickers.Where(x =>
            {
                string key = MakeKey(x, startDate, endDate, field, periodicity);
                return !XLOM.Contains(key);
            }).ToArray();

            if (tickersToRetrieve.Length != 0)
            {
                bool ok = GetBBGData(tickersToRetrieve, startDate, endDate, field, periodicity);

                if (!ok)
                    throw new Exception("Error retrieving data from Bloomberg!");
            }

            // Assemble data.
            Dictionary<string, SortedDictionary<DateTime, double>> data = new Dictionary<string, SortedDictionary<DateTime, double>>();
            SortedSet<DateTime> dates = new SortedSet<DateTime>();

            foreach (string ticker in tickers)
            {
                string key = MakeKey(ticker, startDate, endDate, field, periodicity);
                data[ticker] = XLOM.Get(key) as SortedDictionary<DateTime, double>;

                if (data[ticker] != null)
                    dates.UnionWith(data[ticker].Keys);
            }

            object[,] ret = new object[dates.Count + (noLabels ? 0 : 1), tickers.Length + (noLabels ? 0 : 1)];
            ret[0, 0] = "Date";

            double[] normalisationFactors = new double[tickers.Length];
            for (int x = 0; x < tickers.Length; ++x)
            {
                normalisationFactors[x] = normalise ? data[tickers[x]].First().Value : 1;
            }

            int i = 1;
            foreach (DateTime date in (reverse ? dates.Reverse() : dates))
            {
                ret[i, 0] = date;

                int j = 0;
                foreach (string ticker in tickers)
                {
                    if (i == 1 && !noLabels)
                        ret[0, j + 1] = (names.Length == 0 ? ticker : names[j]);

                    if (data[ticker].ContainsKey(date))
                        ret[i, j + 1] = data[ticker][date] / normalisationFactors[j];
                    else
                        ret[i, j + 1] = data[ticker].Where(x => x.Key < date).OrderBy(x => x.Key).LastOrDefault().Value / normalisationFactors[j];

                    j++;
                }

                i++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get option expiries for Bloomberg")]
        public static object GetOptionExpiries(string Underlying, object ExpiryTypeOpt)
        {
            string expirytype = Utils.GetOptionalParameter(ExpiryTypeOpt, string.Empty);

            string omKey = Underlying + "_" + expirytype + "_Expiries";
            if (XLOM.Contains(omKey))
                return XLOM.Get(omKey);

            session = new Session();
            bool sessionStarted = session.Start();
            if (!sessionStarted || !session.OpenService("//blp/refdata"))
            {
                return "Failed to connect to Bloomberg!";
            }

            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            Element securities = request.GetElement("securities");
            securities.AppendValue(Underlying);

            Element fields = request.GetElement("fields");
            fields.AppendValue("CHAIN_STRUCTURE");

            session.SendRequest(request, null);

            List<DateTime> expiries = new List<DateTime>();
            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    if (msg.HasElement("securityData"))
                    {
                        Element securityData = msg["securityData"].GetValueAsElement(0);
                        Element structureData = (Element)securityData["fieldData"].GetElement(0);
                        for (int i = 0; i < structureData.NumValues; ++i)
                        {
                            string expiry = ((Element)structureData[i])["Expiration"].GetValueAsString();
                            DateTime dt = DateTime.Parse(expiry);

                            if (expirytype == string.Empty || expirytype == "All" || ((Element)structureData[i])["Periodicity"].GetValueAsString() == expirytype)
                                expiries.Add(dt);
                        }
                    }
                }

                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }

            object[,] ret = expiries.Distinct().Select(x => x.ToOADate()).OrderBy(x => x).ToArray().ToRange();
            XLOM.Add(omKey, ret);

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get option strikes for a particular expiry from Bloomberg")]
        public static object GetOptionStrikes(string Underlying, string Expiry)
        {
            string omKey = Underlying + "_" + Expiry + "_Strikes";
            if (XLOM.Contains(omKey))
                return XLOM.Get(omKey);

            object[,] ret = getOptionStrikes(Underlying, Expiry).ToRange();
            XLOM.Add(omKey, ret);

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Gets implied forward for a particular expiry from Bloomberg")]
        public static object GetImpliedForwardCurve(string Underlying, object ExcludeZeroVolumeOptions)
        {
            bool excludeOnZeroVolume = Utils.GetOptionalParameter<bool>(ExcludeZeroVolumeOptions, false);

            string omKey = Underlying + "_ForwardFactors";
            if (XLOM.Contains(omKey))
                return XLOM.Get(omKey);

            SortedDictionary<DateTime, SummaryContainer> forwardFactors = new SortedDictionary<DateTime, SummaryContainer>();

            string type = Underlying.Split(' ').Last();

            // Get call and put tickers.
            session = new Session();
            bool sessionStarted = session.Start();
            if (!sessionStarted || !session.OpenService("//blp/refdata"))
            {
                return "Failed to connect to Bloomberg!";
            }

            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            Element securities = request.GetElement("securities");
            securities.AppendValue(Underlying);

            Element fields = request.GetElement("fields");
            fields.AppendValue("PX_LAST");
            fields.AppendValue("CHAIN_FULL");

            session.SendRequest(request, null);

            double spot = 0;
            List<string> tickers = new List<string>();
            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    if (msg.HasElement("securityData"))
                    {
                        Element securityData = msg["securityData"].GetValueAsElement(0);

                        if (securityData["fieldData"].HasElement("PX_LAST"))
                            spot = securityData["fieldData"].GetElement("PX_LAST").GetValueAsFloat64();

                        Element structureData = (Element)securityData["fieldData"].GetElement("CHAIN_FULL");

                        for (int i = 0; i < structureData.NumValues; ++i)
                        {
                            string expiration = ((Element)structureData[i])["Expiration"].GetValueAsString();
                            DateTime dt = DateTime.Parse(expiration);

                            if (!forwardFactors.ContainsKey(dt))
                                forwardFactors.Add(dt, new SummaryContainer());

                            double strike = ((Element)structureData[i])["Strike"].GetValueAsFloat64();

                            if (spot == 0 || Math.Abs(strike / spot - 1) < 0.3)
                            {
                                string call = ((Element)structureData[i])["Call Ticker"].GetValueAsString();

                                tickers.Add(call);
                            }
                        }
                    }
                }

                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }

            // Get implied forwards.
            if (tickers.Count > 0)
            {
                Request req2 = refDataService.CreateRequest("ReferenceDataRequest");
                Element securities2 = req2.GetElement("securities");
                tickers.ForEach(x => securities2.AppendValue(x + " " + type));

                Element fields2 = req2.GetElement("fields");
                fields2.AppendValue("OPT_EXPIRE_DT");
                fields2.AppendValue("PARITY_FWD_MID");
                fields2.AppendValue("VOLUME");

                session.SendRequest(req2, null);

                while (true)
                {
                    Event eventObj = session.NextEvent();
                    foreach (Message msg in eventObj)
                    {
                        if (msg.HasElement("securityData"))
                        {
                            int numValues = msg["securityData"].NumValues;
                            for (int i = 0; i < numValues; ++i)
                            {
                                Element securityData = msg["securityData"].GetValueAsElement(i);
                                if (securityData.HasElement("fieldData") && securityData["fieldData"].NumElements > 0)
                                {
                                    int volume = 0;
                                    if (securityData["fieldData"].HasElement("VOLUME"))
                                        volume = securityData["fieldData"].GetElement("VOLUME").GetValueAsInt32();

                                    DateTime expiration = DateTime.Now;
                                    if (securityData["fieldData"].HasElement("OPT_EXPIRE_DT")) {
                                        string exp = ((Element)securityData["fieldData"]).GetElement("OPT_EXPIRE_DT").GetValueAsString();
                                        expiration = DateTime.Parse(exp);
                                    }

                                    Element structureData = (Element)securityData["fieldData"].GetElement("PARITY_FWD_MID");

                                    if (structureData.NumValues > 0)
                                    {
                                        double forward = structureData.GetValueAsFloat64();
                                        if (volume > 0 || !excludeOnZeroVolume) {
                                            forwardFactors[expiration].Add(forward / spot);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (eventObj.Type == Event.EventType.RESPONSE)
                    {
                        break;
                    }
                }
            }

            object[,] ret = new object[forwardFactors.Count, 2];
            int j = 0;

            foreach (KeyValuePair<DateTime, SummaryContainer> kv in forwardFactors)
            {
                ret[j, 0] = kv.Key;

                if (kv.Value.Average == 0)
                {
                    ret[j, 1] = ExcelDna.Integration.ExcelError.ExcelErrorNA;
                }
                else
                {
                    ret[j, 1] = kv.Value.Average;
                }

                j++;
            }

            // Save the forward factors.
            XLOM.Add(omKey, ret);

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get BBG ticker based on underlying/expiry/strike/putcall")]
        public static object GetOptionTicker(string Underlying, string Expiry, double Strike, int PutOrCall)
        {
            DateTime expiry;
            if (!DateTime.TryParse(Expiry, out expiry))
            {
                expiry = DateTime.FromOADate(int.Parse(Expiry));
            }

            string type = Underlying.Split(' ').Last();

            string omKey = Underlying + "_OptionTickerTemplate";
            if (XLOM.Contains(omKey))
            {
                string ticker = XLOM.Get(omKey).ToString().Replace("___TYPE___", PutOrCall == -1 ? " P" : " C")
                                                          .Replace("___STRIKE___", Strike.ToString())
                                                          .Replace("___EXPIRY___", expiry.ToString("MM/dd/yy"));
                return ticker;
            }

            session = new Session();
            bool sessionStarted = session.Start();
            if (!sessionStarted || !session.OpenService("//blp/refdata"))
            {
                throw new Exception("Failed to connect to Bloomberg!");
            }

            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            Element securities = request.GetElement("securities");
            securities.AppendValue(Underlying);

            Element fields = request.GetElement("fields");
            fields.AppendValue("CHAIN_FULL");

            session.SendRequest(request, null);

            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    if (msg.HasElement("securityData"))
                    {
                        Element securityData = msg["securityData"].GetValueAsElement(0);
                        Element structureData = (Element)securityData["fieldData"].GetElement(0);
                        for (int i = 0; i < structureData.NumValues; ++i)
                        {
                            double strike = ((Element)structureData[i])["Strike"].GetValueAsFloat64();
                            string expiration = ((Element)structureData[i])["Expiration"].GetValueAsString();
                            DateTime dt = DateTime.Parse(expiration);

                            if (dt == expiry && strike == Strike)
                            {
                                string ticker;
                                if (PutOrCall == -1)
                                    ticker = ((Element)structureData[i])["Put Ticker"].GetValueAsString() + " " + type;
                                else
                                    ticker = ((Element)structureData[i])["Call Ticker"].GetValueAsString() + " " + type;

                                string template = ticker.Replace(dt.ToString("MM/dd/yy"), "___EXPIRY___").Replace(strike.ToString(), "___STRIKE___");
                                template = template.Replace(PutOrCall == -1 ? " P" : " C", " ___TYPE___");
                                XLOM.Add(omKey, template);

                                return ticker;                                
                            }
                        }
                    }
                }

                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }

            return "Error, no such listed option!";
        }


        private static double[] getOptionStrikes(string Underlying, string Expiry)
        {
            DateTime expiry;
            if (!DateTime.TryParse(Expiry, out expiry))
            {
                expiry = DateTime.FromOADate(int.Parse(Expiry));
            }

            session = new Session();
            bool sessionStarted = session.Start();
            if (!sessionStarted || !session.OpenService("//blp/refdata"))
            {
                throw new Exception("Failed to connect to Bloomberg!");
            }

            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            Element securities = request.GetElement("securities");
            securities.AppendValue(Underlying);

            Element fields = request.GetElement("fields");
            fields.AppendValue("CHAIN_FULL");

            session.SendRequest(request, null);

            List<double> strikes = new List<double>();
            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    if (msg.HasElement("securityData"))
                    {
                        Element securityData = msg["securityData"].GetValueAsElement(0);
                        Element structureData = (Element)securityData["fieldData"].GetElement(0);
                        for (int i = 0; i < structureData.NumValues; ++i)
                        {
                            string expiration = ((Element)structureData[i])["Expiration"].GetValueAsString();
                            DateTime dt = DateTime.Parse(expiration);

                            if (dt == expiry)
                            {
                                double strike = ((Element)structureData[i])["Strike"].GetValueAsFloat64();
                                strikes.Add(strike);
                            }
                        }
                    }
                }

                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }

            return strikes.ToArray();
        }


        private static bool GetBBGData(string[] tickers, string startDate, string endDate, string field, string periodicity)
        {
            session = new Session();
            bool sessionStarted = session.Start();
            if (!sessionStarted || !session.OpenService("//blp/refdata"))
            {
                return false;
            }

            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("HistoricalDataRequest");

            Element securities = request.GetElement("securities");
            foreach (string t in tickers)
            {
                securities.AppendValue(t.ToUpper());
            }

            Element fields = request.GetElement("fields");
            fields.AppendValue(field);

            request.Set("periodicityAdjustment", "ACTUAL");
            request.Set("periodicitySelection", periodicity);
            request.Set("nonTradingDayFillOption", "NON_TRADING_WEEKDAYS");

            if (periodicity == "DAILY")
                request.Set("calendarCodeOverride", "5d");

            request.Set("startDate", startDate);

            if (endDate != string.Empty)
                request.Set("endDate", endDate);

            request.Set("returnEids", true);

            session.SendRequest(request, null);

            Dictionary<string, SortedDictionary<DateTime, double>> data = new Dictionary<string, SortedDictionary<DateTime, double>>();
            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    if (msg.HasElement("securityData"))
                    {
                        string ticker = msg["securityData"]["security"].GetValue() as string;
                        if (!data.ContainsKey(ticker))
                            data[ticker] = new SortedDictionary<DateTime, double>();

                        int nData = msg["securityData"]["fieldData"].NumValues;
                        for (int j = 0; j < nData; ++j)
                        {
                            Element oo = (Element)msg.AsElement["securityData"]["fieldData"][j];
                            DateTime dt = oo["date"].GetValueAsDatetime().ToSystemDateTime();

                            if (oo.HasElement(field))
                                data[ticker][dt] = oo[field].GetValue().AsDouble();
                        }
                    }
                }
                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }

            if (data.Count == 0)
                return false;

            foreach (string k in data.Keys)
            {
                string key = MakeKey(k, startDate, endDate, field, periodicity);
                XLOM.Add(key, data[k]);
            }

            return true;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Evaluate expressions using R & BBG! Overrides cumprod/mean/sd/lag/ewma/cond/corr/beta/mround.")]
        public static object ZEval(object[] Expressions, string StartDate, object EndDateOpt,
                                   [ExcelArgument(Description="Use 'TR' to attempt to use total return indices. BBG limited to 5k data points.")]
                                   object FieldOpt, object PeriodicityOpt,
                                   object ReverseOpt, object NoLabelsOpt, object[] NamesOpt)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return "#N/A";

            string[] expressionSet = Utils.GetVector<string>(Expressions);
            string field = Utils.GetOptionalParameter(FieldOpt, "PX_LAST");
            if (field.ToLower().StartsWith("tr"))
                field = "TOT_RETURN_INDEX_NET_DVDS";

            string periodicity = Utils.GetOptionalParameter(PeriodicityOpt, "Daily").ToUpper();

            string startDate = Utils.GetDate(StartDate, DateTime.Now).ToString("yyyyMMdd");
            string endDate = Utils.GetDate(EndDateOpt, DateTime.Now).ToString("yyyyMMdd");

            bool reverse = Utils.GetOptionalParameter(ReverseOpt, false);
            bool noLabels = Utils.GetOptionalParameter(NoLabelsOpt, false);
            string[] names = Utils.GetVector<string>(NamesOpt);

            Stopwatch sw1 = new Stopwatch();

            sw1.Start();

            // Identify tickers.
            Regex regex = new Regex(@"(?<=\{).*?(?=\})");
            Dictionary<string, string> tickersToVariables = new Dictionary<string,string>();

            // We're going to decorate naked expressions with curlies UNLESS the expression set contains '=' signs, which signify
            // variable declarations.
            bool needsBracesDefault = true;
            if (expressionSet.Where(x => x.Contains('=')).Count() > 0)
                needsBracesDefault = false;

            int i = 0;
            foreach (string expression in expressionSet)
            {
                bool needsBraces = needsBracesDefault;

                foreach (Match match in regex.Matches(expression))
                {
                    string ticker = match.Value;
                    if (ticker.Contains("r@") || ticker.Contains("d@") || ticker.Contains("l@"))
                    {
                        ticker = ticker.Replace("r@", "");
                        ticker = ticker.Replace("d@", "");
                        ticker = ticker.Replace("l@", "");
                    }

                    needsBraces = false;

                    if (tickersToVariables.ContainsKey(ticker))
                        continue;
                    else
                        tickersToVariables[ticker] = "group" + (i++);
                }

                // If no matches found, assume the whole expression is one ticker.
                if (needsBraces)
                {
                    expressionSet[i] = "{" + expression + "}";
                    tickersToVariables[expression] = "group" + (i++);
                }
            }

            // Assemble raw data.
            string[] tickersToRetrieve = tickersToVariables.Keys.Where(x =>
            {
                string key = MakeKey(x, startDate, endDate, field, periodicity);
                return !XLOM.Contains(key);
            }).ToArray();

            if (tickersToRetrieve.Length != 0)
            {
                bool ok = GetBBGData(tickersToRetrieve, startDate, endDate, field, periodicity);

                if (!ok)
                    throw new Exception("Error retrieving data from Bloomberg!");
            }

            Debug.Write("Got data in " + sw1.ElapsedMilliseconds + "ms; ");
            sw1.Restart();

            Dictionary<string, SortedDictionary<DateTime, double>> data = new Dictionary<string, SortedDictionary<DateTime, double>>();
            SortedSet<DateTime> dates = new SortedSet<DateTime>();

            DateTime firstDate = new DateTime(9999, 1, 1);
            foreach (string ticker in tickersToVariables.Keys)
            {
                string key = MakeKey(ticker, startDate, endDate, field, periodicity);
                data[ticker] = XLOM.Get(key) as SortedDictionary<DateTime, double>;

                DateTime startDate1 = data[ticker].Keys.FirstOrDefault();
                if (startDate1 < firstDate)
                    firstDate = startDate1;

                if (data[ticker] != null)
                    dates.UnionWith(data[ticker].Keys.Where(x => x >= firstDate));
            }

            dates.RemoveWhere(x => x < firstDate);

            Debug.Write("assembled data in " + sw1.ElapsedMilliseconds + "ms; ");
            sw1.Restart();

            return ZEvalOnData(expressionSet, reverse, noLabels, names, sw1, tickersToVariables, data, dates);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Evaluate expressions using R on user-provided data! Overrides cumprod/mean/sd/lag/ewma/cond/corr/beta/mround.")]
        public static object ZEvalOnData(object[,] Data, object[] VariableNames, object[] Expressions, object ReverseOpt, object NoLabelsOpt, object[] NamesOpt)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return "#N/A";

            string[] expressionSet = Utils.GetVector<string>(Expressions);
            string[] variableNames = Utils.GetVector<string>(VariableNames);
            Dictionary<string, string> tickersToVariables = variableNames.ToDictionary<string, string>(x => x);

            // Assemble data.
            double[,] rawData = Utils.GetMatrix<double>(Data);
            SortedSet<DateTime> dates = new SortedSet<DateTime>();
            int nDates = rawData.GetLength(0);
            DateTime start = DateTime.Today.AddDays(-nDates);
            for (int i = 0; i < nDates; ++i)
            {
                dates.Add(start.AddDays(i));
            }

            Dictionary<string, SortedDictionary<DateTime, double>> data = new Dictionary<string, SortedDictionary<DateTime, double>>();
            for (int i = 0; i < variableNames.Length; ++i)
            {
                SortedDictionary<DateTime, double> sd = new SortedDictionary<DateTime, double>();
                int j = 0;
                foreach (DateTime d in dates) {
                    sd.Add(d, rawData[j++, i]);
                }

                variableNames[i] = variableNames[i].ToLower();
                data.Add(variableNames[i], sd);
            }

            bool reverse = Utils.GetOptionalParameter(ReverseOpt, false);
            bool noLabels = Utils.GetOptionalParameter(NoLabelsOpt, false);
            string[] names = Utils.GetVector<string>(NamesOpt);

            Stopwatch sw1 = new Stopwatch();

            sw1.Start();

            return ZEvalOnData(expressionSet, reverse, noLabels, names, sw1, tickersToVariables, data, dates);
        }



        private static object ZEvalOnData(string[] expressionSet, bool reverse, bool noLabels, string[] names,
                                          Stopwatch sw1, Dictionary<string, string> tickersToVariables,
                                          Dictionary<string, SortedDictionary<DateTime, double>> data, SortedSet<DateTime> dates)
        {
            // Set up vectors in R.
            REngine.SetEnvironmentVariables();
            REngine engine = REngine.GetInstance();

            // Load basic script.
            engine.Evaluate("source('" + XL.ZEvalScript.Replace('\\', '/') + "')");

            foreach (string ticker in tickersToVariables.Keys)
            {
                List<double> data1 = new List<double>();

                foreach (DateTime dt in dates)
                {
                    if (data[ticker].ContainsKey(dt))
                        data1.Add(data[ticker][dt]);
                    else
                        data1.Add(data[ticker].Where(x => x.Key < dt).OrderBy(x => x.Key).LastOrDefault().Value);
                }

                NumericVector group = engine.CreateNumericVector(data1);
                engine.SetSymbol(tickersToVariables[ticker], group);
            }

            Debug.Write("prepped R in " + sw1.ElapsedMilliseconds + "ms; ");
            sw1.Restart();

            // Now evaluate each expression.
            Dictionary<string, SortedDictionary<DateTime, double>> data2 = new Dictionary<string, SortedDictionary<DateTime, double>>();
            foreach (string expression in expressionSet)
            {
                string expr_mod = expression;

                if (expr_mod[0] == '*')
                {
                    expr_mod = "cumprod(1 + " + expr_mod.Substring(1) + ")";
                }

                foreach (string ticker in tickersToVariables.Keys)
                {
                    expr_mod = expr_mod.Replace("{" + ticker + "}", tickersToVariables[ticker]);

                    expr_mod = expr_mod.Replace("{r@" + ticker + "}", "percentage_returns(" + tickersToVariables[ticker] + ")");
                    expr_mod = expr_mod.Replace("{d@" + ticker + "}", "normal_returns(" + tickersToVariables[ticker] + ")");
                    expr_mod = expr_mod.Replace("{l@" + ticker + "}", "log_returns(" + tickersToVariables[ticker] + ")");
                }

                try
                {
                    var result = engine.Evaluate(expr_mod.ToLower());
                    var values = result.AsNumeric();

                    data2[expression] = new SortedDictionary<DateTime, double>();

                    int k = 0;
                    foreach (DateTime dt in dates.Reverse())
                    {
                        data2[expression][dt] = values[values.Length - k - 1];

                        k++;
                    }
                }
                catch (Exception e)
                {
                    string error = expression + " ------- " + e.ToString();
                    Debug.WriteLine(error);
                    return error;
                }
            }

            Debug.Write("ran R in " + sw1.ElapsedMilliseconds + "ms; ");
            sw1.Restart();

            object[,] ret = new object[dates.Count + (noLabels ? 0 : 1), expressionSet.Length + (noLabels ? 0 : 1)];
            ret[0, 0] = "Date";

            int i = 1;
            foreach (DateTime date in (reverse ? dates.Reverse() : dates))
            {
                ret[i, 0] = date;

                int j = 0;
                foreach (string expression in expressionSet)
                {
                    if (i == 1 && !noLabels)
                        ret[0, j + 1] = (names.Length < j + 1 ? expression : names[j]);

                    ret[i, j + 1] = data2[expression][date];
                    j++;
                }

                i++;
            }

            Debug.WriteLine("finished in " + sw1.ElapsedMilliseconds + "ms; ");

            return ret;
        }


        private static string MakeKey(string ticker, string startDate, string endDate, string field, string periodicity)
        {
            return ticker.ToUpper() + "_" + startDate + "_" + endDate + "_" + field + "_" + periodicity;
        }
    }
}
