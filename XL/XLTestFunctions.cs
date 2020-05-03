using System;
using System.Collections.Generic;
using System.Diagnostics;

using ExcelDna.Integration;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Distributions.Univariate;
using ql = QLNet;
using CommonTypes;
using CommonTypes.Maths;
using RDotNet;
using DataSources;

namespace XL
{
    public class XLTestFunctions
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Test Accord's regression model")]
        public static object TestRegression(double[] x, double[] y)
        {
            SimpleLinearRegression slr = new SimpleLinearRegression();

            double err = slr.Regress(x, y);

            double[] values = slr.Compute(x);

            object[,] ret = new object[values.Length + 3, 2];
            ret[0, 0] = "R^2";
            ret[0, 1] = slr.CoefficientOfDetermination(x, y);
            ret[1, 0] = "Slope";
            ret[1, 1] = slr.Slope;
            ret[2, 0] = "Error";
            ret[2, 1] = err;
            for (int i = 0; i < values.Length; ++i)
            {
                ret[i + 3, 0] = x[i];
                ret[i + 3, 1] = values[i];
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Fit Poisson")]
        public static object TestPoisson(double lambda, double[] x, double[] y)
        {
            PoissonDistribution dist = new PoissonDistribution(lambda);

            dist.Fit(y);

            double[] ret = new double[4];

            ret[0] = dist.Entropy;
            ret[1] = dist.Mean;
            ret[2] = dist.Variance;
            ret[3] = dist.StandardDeviation;

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test QLNet calendars")]
        public static object TestCalendars(string CalendarName, DateTime StartDate, DateTime EndDate)
        {
            Type tt = CommonTypes.Utils.FindType(CalendarName, null);
            ql.Calendar cal = (ql.Calendar)Activator.CreateInstance(tt);

            int nDays = (int)(EndDate - StartDate).TotalDays;
            object[,] ret = new object[nDays, 2];

            for (int i = 0; i < nDays; ++i) {
                DateTime date = StartDate.AddDays(i);
                ret[i, 0] = date.ToOADate();
                ret[i, 1] = cal.isHoliday(new ql.Date(date));
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test QLNet B-S")]
        public static object TestBS(double Spot, double DivYield, double Vol, double T, double DF, double Strike)
        {
            CommonTypes.Maths.BlackScholes bb = new BlackScholes(CallPut.Call, T, DF, Vol, DivYield, Spot, Strike);

            object[,] ret = new object[2, 2];

            double premium = bb.Premium();
            double delta = bb.Delta();

            ret[0, 0] = "Premium"; ret[0, 1] = premium;
            ret[1, 0] = "Delta"; ret[1, 1] = delta;

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test SummaryContainer")]
        public static object TestSummaryContainer(double[] data)
        {
            object[,] ret = new object[data.Length, 6];
            SummaryContainer sc = new SummaryContainer();

            int i = 0;
            foreach (double d in data)
            {
                sc.Add(d);

                ret[i, 0] = sc.Count;
                ret[i, 1] = sc.Min;
                ret[i, 2] = sc.Max;
                ret[i, 3] = sc.Average;
                ret[i, 4] = sc.Stdev;
                ret[i, 5] = sc.Total;

                i++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test VarBars")]
        public static object TestVarBars(double[] Data, int NumBarsToStore, int BarLengthInSeconds)
        {
            SortedDictionary<DateTimeOffset, Dictionary<string, double>> Values = new SortedDictionary<DateTimeOffset,Dictionary<string,double>>();

            int epochSecs = 1;
            IMessageBus Ether = new Ether(epochSecs);

            BarList bl = new BarList(Ether, 1, NumBarsToStore, (int)(BarLengthInSeconds));
            bl.BarFinalisedListeners += (s, e) => {
                DateTimeOffset closeTime = e.EndTime;
                if (!Values.ContainsKey(closeTime))
                    Values[closeTime] = new Dictionary<string, double>();

                Values[closeTime]["Open"] = (double)e.Open;
                Values[closeTime]["Low"] = (double)e.Low;
                Values[closeTime]["High"] = (double)e.High;
                Values[closeTime]["Close"] = (double)e.Close;
                Values[closeTime]["NumTicks"] = e.NumTicks;
            };

            VarBars vb = new VarBars(Ether, 1, NumBarsToStore, BarLengthInSeconds);
            vb.BarFinalisedListeners += (s, e) => {
                if (!Values.ContainsKey(e.EndTime))
                    Values[e.EndTime] = new Dictionary<string, double>();

                Values[e.EndTime]["VB_Open"] = (double)e.Open;
                Values[e.EndTime]["VB_Low"] = (double)e.Low;
                Values[e.EndTime]["VB_High"] = (double)e.High;
                Values[e.EndTime]["VB_Close"] = (double)e.Close;
                Values[e.EndTime]["VB_Average"] = 0;// (double)e.Average;
                Values[e.EndTime]["VB_MaxSpread"] = 0; //(double)e.MaxSpread;
                Values[e.EndTime]["VB_AverageSpread"] = 0; //(double)e.AverageSpread;
                Values[e.EndTime]["VB_NumTicks"] = e.NumTicks;
            };

            DateTimeOffset dto = DateTimeOffset.UtcNow;
            dto = new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, new TimeSpan(0));

            int i = 0;
            foreach (double d in Data)
            {
                DateTimeOffset time = dto.AddSeconds(i * epochSecs);
                Market mkt = new Market(time, 1, (decimal)(d * 0.999), (decimal)(d * 1.001));
                Ether.Send(mkt);

                Ether.Send(new Heartbeat(time, epochSecs, "Heartbeat"));
                i++;
            }

            object[,] ret = new object[Values.Count, 15];
            i = 0;
            foreach (DateTimeOffset k in Values.Keys)
            {
                ret[i, 0] = k.ToString("yy-MM-dd HH:mm:ss.fff");

                if (Values[k].ContainsKey("Open"))
                {
                    ret[i, 1] = Values[k]["Open"];
                    ret[i, 2] = Values[k]["Low"];
                    ret[i, 3] = Values[k]["High"];
                    ret[i, 4] = Values[k]["Close"];
                    ret[i, 5] = Values[k]["NumTicks"];
                }
                else
                {
                    ret[i, 1] = ret[i, 2] = ret[i, 3] = ret[i, 4] = ret[i, 5] = "";
                }

                ret[i, 6] = "";

                if (Values[k].ContainsKey("VB_Open"))
                {
                    ret[i, 7] = Values[k]["VB_Open"];
                    ret[i, 8] = Values[k]["VB_Low"];
                    ret[i, 9] = Values[k]["VB_High"];
                    ret[i, 10] = Values[k]["VB_Close"];
                    ret[i, 11] = Values[k]["VB_Average"];
                    ret[i, 12] = Values[k]["VB_MaxSpread"];
                    ret[i, 13] = Values[k]["VB_AverageSpread"];
                    ret[i, 14] = Values[k]["VB_NumTicks"];
                }
                else
                {
                    ret[i, 7] = ret[i, 8] = ret[i, 9] = ret[i, 10] = ret[i, 11] = ret[i, 12] = ret[i, 13] = ret[i, 14] = "";
                }

                i++;
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test in-memory BarSource")]
        public static object TestDifferentialEvolution(int popSize, int nIters, double diffWeight, double crossProb, object makeOneParameterIntegerOpt, object stoppingEpsilonOpt)
        {
            bool makeInteger = Utils.GetOptionalParameter(makeOneParameterIntegerOpt, false);
            double stoppingEpsilon = Utils.GetOptionalParameter(stoppingEpsilonOpt, 1e-10);

            DifferentialEvolution o = new Sinc2DOptimizer(2, popSize, diffWeight, crossProb);

            double[,] bounds = new double[2, 2];
            bounds[0, 0] = -2 * 3.14159265359; bounds[0, 1] = 2 * 3.14159265359;
            bounds[1, 0] = -2 * 3.14159265359; bounds[1, 1] = 2 * 3.14159265359;

            string[] parameterTypes = new string[2];
            parameterTypes[0] = "Real";
            parameterTypes[1] = (makeInteger ? "Integer" : "Real");

            DateTime start = DateTime.Now;
            Tuple<double, double[]> results = o.Optimise(1, bounds, parameterTypes, nIters, stoppingEpsilon);
            double runtime = (DateTime.Now - start).TotalMilliseconds;
            Debug.WriteLine("DE Iterations: " + (o.Iteration - 1) + ", Runtime: " + runtime + "ms");

            double[,] agents = o.GetAgents();

            double[,] ret = new double[1 + popSize, 2];
            ret[0, 0] = results.Item2[0];
            ret[0, 1] = results.Item2[1];

            for (int i = 1; i < 1 + popSize; ++i)
            {
                ret[i, 0] = agents[i - 1, 0];
                ret[i, 1] = agents[i - 1, 1];
            }    

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test rounding")]
        public static object TestRounding(double input, int numDigits)
        {
            return Math.Round(input, numDigits, MidpointRounding.AwayFromZero);
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Find a change point")]
        public static object FindChangePoint(double[] X, double[] Y, object PrintLabelsOpt)
        {
            bool printLabels = Utils.GetOptionalParameter(PrintLabelsOpt, true);

            BayesianChangePoints bcp = new BayesianChangePoints(X, Y);

            object[,] ret = null;
            if (bcp.Test() != -1)
            {
                int o = printLabels ? 1 : 0;
                ret = new object[5, 1 + o];

                if (printLabels)
                {
                    ret[0, 0] = "Index";
                    ret[1, 0] = "X-value";
                    ret[2, 0] = "Constant";
                    ret[3, 0] = "Trend1";
                    ret[4, 0] = "Trend2";
                }

                ret[0, o] = bcp.ChangePointIndex;
                ret[1, o] = X[bcp.ChangePointIndex];
                ret[2, o] = bcp.Constant;
                ret[3, o] = bcp.Trend1;
                ret[4, o] = bcp.Trend2;
            }
            else
            {
                ret = new object[1, 1];
                ret[0, 0] = "No changepoint!";
            }

            return ret;
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Create a small scenario cube")]
        public static object TestBSScenarios(int nAssets, object nPathsOpt)
        {
            int nPaths = Utils.GetOptionalParameter(nPathsOpt, 1);

            double r = 0.01;

            double[] spots = new double[nAssets];
            double[] q = new double[nAssets];
            double[] vols = new double[nAssets];
            double[,] corrs = new double[nAssets, nAssets];
            int nSteps = 8;
            double dt = 0.25;

            for (int i = 0; i < nAssets; ++i)
            {
                spots[i] = 100;
                q[i] = 0.0;
                vols[i] = 0.25;
                corrs[i, i] = 1;
            }

            ScenarioCube sc = new ScenarioCube(r, spots, q, vols, corrs, nSteps, dt);

            if (nPaths == 1)
                return sc.Next();
            else
            {
                object[,] ret = new object[nPaths, (nSteps + 1) * (nAssets + 1) - 1];
                ret.FillWithEmptyStrings();

                for (int i = 0; i < nPaths; ++i)
                {
                    double[,] path = sc.Next();

                    for (int j = 0; j < nSteps; ++j)
                    {
                        for (int k = 0; k < nAssets; ++k)
                        {
                            ret[i, k * (nSteps + 1) + j] = path[j, k];
                        }

                        ret[i, nAssets * (nSteps + 1) + j] = path[j, nAssets];
                    }
                }

                return ret;
            }
        }

        [ExcelFunction(Category = "ZeusXL", Description = "Just test")]
        public static object TestFunction()
        {
            return "this is a test";
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Creates a histogram")]
        public static object TestR(double[] data, string expression)
        {
            REngine.SetEnvironmentVariables();
            // There are several options to initialize the engine, but by default the following suffice:
            REngine engine = REngine.GetInstance();

            // .NET Framework array to R vector.
            NumericVector group1 = engine.CreateNumericVector(data);
            engine.SetSymbol("group1", group1);
            // Direct parsing from R script.
            var result = engine.Evaluate(expression);

            return result.AsNumericMatrix().ToArray();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Test IB Api")]
        public static object TestGetStockPrices(string ticker)
        {
            Contract cc = new Contract(1, ticker, "STK", "SMART", "USD", "", 100, "", 0, "", "", "", "", new PositiveInteger(1));

            Ether privateEther = new Ether(60);
            IBSource tws = new IBSource(privateEther, "127.0.0.1", 7496, 1, "DU74885", false, 60);
            tws.Connect();

            tws.Subscribe(cc);

            double[] ret = new double[6];
            privateEther.AsObservable<Market>().Subscribe(m =>
            {
                ret[0] = m.BidSize;
                ret[1] = (double)m.Bid;
                ret[2] = (double)m.Mid;
                ret[3] = (double)m.Ask;
                ret[4] = m.AskSize;
            });

            return ret;

            tws.Unsubscribe(cc);
        }

    }
}
