using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Globalization;

using CommonTypes;
using CommonTypes.BrokerUtils;


// TODO(live-3) - request real-time bars for time-and-sales data.
namespace DataSources
{
    public class TWSSource : IEnumerable<ITimestampedDatum>
    {
        public IMessageBus Ether;

        public readonly string Hostname;
        public readonly int Port;
        public readonly int ClientId;
        public readonly string AccountName;

        public readonly bool UseVarBars;
        public readonly int BarLength;

        private IBClient TWSClient;
        private int CurrentOrderId;

        private DateTime StartTime = DateTime.MinValue;
        private double TimeOffset = 0;

        public Dictionary<int, Tuple<Contract, Market>> Contracts;
        public Dictionary<int, IBarList> Bars;

        public AccountInfo AccountInfo;


        public TWSSource(IMessageBus ether, string hostname, int port, int clientId, string accountName, bool useVarBars, int barLength)
        {
            Ether = ether;

            Hostname = hostname;
            Port = port;
            ClientId = clientId;
            AccountName = accountName;

            UseVarBars = useVarBars;
            BarLength = barLength;

            if (BarLength < Ether.EpochSecs)
                throw new Exception(string.Format("Error, TWSSource bar length ({0}) is less than the Ether's epoch interval ({1})!", BarLength, ether.EpochSecs));

            TWSClient = new IBClient();

            // Contract details, currently just market hours.
            TWSClient.ContractDetails += ContractDetailsHandler;

            // Tick and historical data.
            TWSClient.TickPrice += TickPriceHandler;
            TWSClient.TickSize += TickSizeHandler;
            TWSClient.TickGeneric += TickGenericHandler;
            TWSClient.TickString += TickStringHandler;
            TWSClient.TickOption += TickOptionHandler;
            TWSClient.HistoricalData += HistoricalDataHandler;
            TWSClient.TickEfp += EFPDataHandler;

            // Account and portfolio updates.
            TWSClient.UpdateAccountTime += AccountTimeHandler;
            TWSClient.UpdateAccountValue += AccountValueHandler;
            TWSClient.UpdatePortfolio += PortfolioInfoHandler;

            // Errors.
            TWSClient.Error += ErrorHandler;

            // Server time.
            TWSClient.CurrentTime += CurrentTimeHandler;

            // Instance variables.
            Contracts = new Dictionary<int, Tuple<Contract, Market>>();
            Bars = new Dictionary<int, IBarList>();
            AccountInfo = new AccountInfo(AccountName);

            // Daily things-to-do.
            Ether.AsObservable<Heartbeat>().Where(x => x.IsHourly()).Subscribe(x =>
            {
                // At 5AM each day, we request contract details so we can see when the contracts are traded.
                foreach (var c in Contracts.Keys)
                    TWSClient.RequestContractDetails(c * 9999, Contracts[c].Item1);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });
        }


        public void Connect()
        {
            try
            {
                TWSClient.Connect(Hostname, Port, ClientId);
                //TWSClient.ThrowExceptions = false;

                //TWSClient.RequestIds(1);
                TWSClient.RequestCurrentTime();
                TWSClient.RequestAccountUpdates(true, AccountName);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }


        public void Subscribe(Contract contract)
        {
            if (!TWSClient.Connected)
                Connect();

            // Add this contract to our list of contracts.
            Contracts[contract.Id] = new Tuple<Contract, Market>(contract, new Market(Now, contract.Id, decimal.MinValue, decimal.MinValue));

            // Set up bars for this contract.
            if (UseVarBars)
                Bars[contract.Id] = new VarBars(Ether, contract.Id, 10, BarLength);
            else
                Bars[contract.Id] = new BarList(Ether, contract.Id, 10, BarLength);

            // And arrange to send out finalised bars.
            Bars[contract.Id].BarFinalisedListeners += (s, b) =>
            {
                Debug.WriteLine(b.ToString());
                Ether.Send(b);
            };

            // Now actually subscribe...
            Contract c = contract;
            try
            {
                TWSClient.RequestMarketData(contract.Id, c, null, false, false);

                // Request opening hours etc.
                // Note: this means an upper limit of 9999 contracts at any given time.
                TWSClient.RequestContractDetails(contract.Id * 9999, c);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }


        public void Unsubscribe(Contract contract)
        {
            if (TWSClient.Connected)
            {
                try
                {
                    TWSClient.CancelMarketData(contract.Id);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }


        void TickPriceHandler(object sender, Dictionary<string, object> data)
        {
            int contractId = (int)data["ContractId"];
            TickType type = (TickType)data["Type"];
            dynamic price = data["Value"];

            //Debug.WriteLine("ZEUS - TickPriceHandler: " + contractId + ", " + type + ", " + price);

            Market currentMarket = Contracts[contractId].Item2;
            currentMarket.Timestamp = Now;

            bool send = false;
            if (type == TickType.BidPrice && price > 0)
            {                       // TODO(live-5) - true for EFPs?
                currentMarket.Bid = price;

                if (Math.Abs(price / currentMarket.Bid - 1) < 0.15M)
                {
                    send = true;
                }
            }
            else if (type == TickType.AskPrice && price > 0)
            {
                currentMarket.Ask = price;

                if (Math.Abs(price / currentMarket.Ask - 1) < 0.15M)
                {
                    send = true;
                }
            }
            else if (currentMarket.Mid.Equals(decimal.MinValue) && (type == TickType.ClosePrice || type == TickType.LastPrice))
            {
                currentMarket.Ask = currentMarket.Bid = price;
                send = true;
            }
            else if (type == TickType.ClosePrice)
            {
                currentMarket.PreviousClose = price;
                send = true;
            }

            if (send)
                Ether.Send(currentMarket);
        }


        void TickSizeHandler(object sender, Dictionary<string, object> data)
        {
            int contractId = (int)data["ContractId"];
            TickType type = (TickType)data["Type"];
            dynamic size = data["Value"];

            //Debug.WriteLine("ZEUS - TickSizeHandler: " + contractId + ", " + type + ", " + size);

            Market currentMarket = Contracts[contractId].Item2;
            currentMarket.Timestamp = Now;

            bool send = false;
            if (type == TickType.BidSize)
            {
                currentMarket.BidSize = size;
                send = true;
            }
            else if (type == TickType.AskSize)
            {
                currentMarket.AskSize = size;
                send = true;
            }

            if (send)
                Ether.Send(currentMarket);
        }


        void ErrorHandler(object sender, Dictionary<string, object> data)
        {
            if (!TWSClient.Connected)
                Connect();

            Debug.WriteLine(data["Exception"].ToString());
        }


        void CurrentTimeHandler(object sender, Dictionary<string, object> data)
        {
            StartTime = (DateTime)data["Time"];
            TimeOffset = (StartTime - DateTime.UtcNow).TotalMilliseconds;
        }


        void TWSNextValidIdHandler(object sender, Dictionary<string, object> data)
        {
            CurrentOrderId = (int)data["OrderId"];
        }


        void TickOptionHandler(object sender, Dictionary<string, object> data)
        {
            int contractId = (int)data["ContractId"];
            Contracts[contractId].Item2.OptionData = (OptionData)data["OptionData"];
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            throw new NotImplementedException("Error, TWSSource isn't meant to be used through the DataCollator!");
        }
        

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException("Error, TWSSource isn't meant to be used through the DataCollator!");
        }


        // TODO(always-1) - USE THIS!!
        public DateTime Now
        {
            get
            {
                return DateTime.UtcNow.AddMilliseconds(TimeOffset);
            }
        }


        // TODO(live-3) -------------------------------------------
        void TickGenericHandler(object sender, Dictionary<string, object> data)
        {
            //Debug.WriteLine("ZEUS - TickGenericHandler: " + e.TickerId + ", " + Enum.GetName(typeof(TickType), e.TickType) + ", " + e.Value);
        }


        // TODO(live-3) -------------------------------------------
        void TickStringHandler(object sender, Dictionary<string, object> data)
        {
            //Debug.WriteLine("ZEUS - TickStringHandler: " + e.TickerId + ", " + Enum.GetName(typeof(TickType), e.TickType) + ", " + e.Value);
        }


        void HistoricalDataHandler(object sender, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }


        void EFPDataHandler(object sender, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }


        void AccountTimeHandler(object sender, Dictionary<string, object> data)
        {
            // TODO(live-5)
        }


        void AccountValueHandler(object sender, Dictionary<string, object> data)
        {
            string accountName = (string)data["AccountName"];
            string key = (string)data["Key"];
            string currency = (string)data["Currency"];
            decimal value = (decimal)data["Value"];
            //Debug.WriteLine("ZEUS - AccountValueHandler: {0} => {1} is worth {2} {3}", e.AccountName, e.Key, e.Currency, e.Value);

            if (accountName == AccountInfo.AccountName)
            {
                if (key == "NetLiquidation")
                {
                    AccountInfo.BaseCurrency = currency;
                    AccountInfo.NetLiquidationValue = value;
                }
                else if (key == "FullInitMarginReq")
                {
                    AccountInfo.BaseCurrency = currency;
                    AccountInfo.InitialMarginRequirement = value;
                }
                else if (key == "FullMaintMarginReq")
                {
                    AccountInfo.BaseCurrency = currency;
                    AccountInfo.MaintenanceMarginRequirement = value;
                }
                else if (key == "GrossPositionValue")
                {
                    AccountInfo.BaseCurrency = currency;
                    AccountInfo.GrossPositionValue = value;
                }
                else if (key == "Leverage-S")
                {                               // TODO(cleanup-5)
                    AccountInfo.BaseCurrency = currency;
                    AccountInfo.Leverage = value;
                }

                AccountInfo.Timestamp = Now;
                if (AccountInfo.IsPrimed)
                    Ether.Send(AccountInfo);
            }
        }


        // This provides information about each position currently held by the account.
        void PortfolioInfoHandler(object sender, Dictionary<string, object> data)
        {
            // TODO(live-3) - this should send out an event that only PMS picks up, and that too optionally (ie, backtesting doesn't
            // emit it). PMS could then check that it's internal state matches that in TWS.
        }


        // TODO(live-1) - figure out how to serve these. There's a MarketHours type that should be used -- Strategy already does
        // something for this.
        // One idea would be to move the DataCollator from LiveRunner to TWSSource, and then have a MarketEventDataSource that I can
        // "load" trading hours updates into. Then the DataCollator would serve the updates at the right time.
        void ContractDetailsHandler(object sender, Dictionary<string, object> data)
        {
            //int contractId = e.RequestId / 9999;

            //// TODO(live-3) - time zones?
            //List<Tuple<DateTimeOffset, int>> liquidHoursList = ParseTradingHoursString(e.ContractDetails.LiquidHours, Now.Date);
            //List<Tuple<DateTimeOffset, int>> tradingHoursList = ParseTradingHoursString(e.ContractDetails.TradingHours, Now.Date);
        }


        public decimal CurrentAccountValue
        {
            get
            {
                return AccountInfo.NetLiquidationValue;
            }
        }


        public string BaseCurrency
        {
            get
            {
                return AccountInfo.BaseCurrency;
            }
        }


        // "20110621:0000-1700,1715-2359;20110622:0000-1700,1715-2359"
        // "20120819:CLOSED;20120820:0000-1700,1715-2359"                                       // AUDUSD
        // "20120820:CLOSED;20120821:0700-1515"                                                 // VIX futures
        // "20120820:CLOSED;20120821:0830-1515"                                                 // ES futures
        // "20110621:1715-1700;20110622:1715-1700"                                              // this means 1715 on the previous day to 1700 today.
        private static List<Tuple<DateTimeOffset, int>> ParseTradingHoursString(string tradingHoursString, DateTime todaysDate)
        {
            List<Tuple<DateTimeOffset, int>> ret = new List<Tuple<DateTimeOffset, int>>();

            string[] tradingHours = tradingHoursString.Split(';');
            foreach (string s in tradingHours)
            {
                string[] tokens = s.Split(':');

                DateTime dt = DateTime.ParseExact(tokens[0], "yyyyMMdd", CultureInfo.InvariantCulture);
                if (dt.Date == todaysDate.Date)
                {
                    if (tokens[1] == "CLOSED")
                        continue;

                    string[] tokens2 = tokens[1].Split(',');
                    foreach (string ss in tokens2)
                    {
                        int startHour = int.Parse(ss.Substring(0, 2));
                        int startMinute = int.Parse(ss.Substring(2, 2));

                        int endHour = int.Parse(ss.Substring(5, 2));
                        int endMinute = int.Parse(ss.Substring(7, 2));

                        DateTime startTime = dt.AddHours(startHour).AddMinutes(startMinute);
                        DateTime endTime = dt.AddHours(endHour).AddMinutes(endMinute);

                        if (startTime > endTime)
                            startTime = startTime.AddDays(-1);

                        ret.Add(new Tuple<DateTimeOffset, int>(startTime, 1));
                        ret.Add(new Tuple<DateTimeOffset, int>(endTime, 0));
                    }
                }
                else
                {
                    continue;
                }
            }

            return ret;
        }
    }
}

// TODO(connectivity) - http://epchan.blogspot.ca/2012/03/high-frequency-trading-in-foreign.html