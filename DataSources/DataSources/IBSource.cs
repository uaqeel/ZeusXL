using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Globalization;

using CommonTypes;
using IB = IBApi;
using CommonTypes.Messages;
using System.Threading;

// TODO(live-3) - request real-time bars for time-and-sales data.
namespace DataSources
{
    public class IBSource : IEnumerable<ITimestampedDatum>
    {
        public IMessageBus Ether;

        public readonly string Hostname;
        public readonly int Port;
        public readonly int ClientId;
        public readonly string AccountName;

        public readonly bool UseVarBars;
        public readonly int BarLength;

        private bool IsConnected;
        private IBClient Client;
        private IB.EReaderMonitorSignal Signal;
        private IB.EReader Reader;
        private int CurrentOrderId;

        private DateTime StartTime = DateTime.MinValue;
        private double TimeOffset = 0;

        public Dictionary<int, Tuple<Contract, Market>> Contracts;
        public Dictionary<int, IBarList> Bars;

        public AccountInfo AccountInfo;


        public IBSource(IMessageBus ether, string hostname, int port, int clientId, string accountName, bool useVarBars, int barLength)
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

            // Set synch context for IBClient
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            IsConnected = false;
            Signal = new IB.EReaderMonitorSignal();
            Client = new IBClient(Signal);

            // Contract details, currently just market hours.
            Client.ContractDetails += ContractDetailsHandler;
            Client.ContractDetailsEnd += ( reqId => Debug.WriteLine(reqId) );

            // Tick data.
            Client.TickPrice += TickPriceHandler;
            Client.TickSize += TickSizeHandler;
            Client.TickGeneric += TickGenericHandler;
            Client.TickString += TickStringHandler;
            Client.TickOptionCommunication += TickOptionHandler;
            Client.TickEFP += EFPDataHandler;

            // Historical data.
            Client.HistoricalData += HistoricalDataHandler;
            Client.HistoricalDataUpdate += HistoricalDataUpdateHandler;
            Client.HistoricalDataEnd += HistoricalDataEndHandler;


            // Account and portfolio updates.
            Client.UpdateAccountTime += AccountTimeHandler;
            Client.UpdateAccountValue += AccountValueHandler;
            Client.UpdatePortfolio += PortfolioInfoHandler;

            // Errors.
            Client.Error += ErrorHandler;
            Client.ConnectionClosed += ConnectionClosedHandler;

            // Server time.
            Client.CurrentTime += CurrentTimeHandler;

            // Instance variables.
            Contracts = new Dictionary<int, Tuple<Contract, Market>>();
            Bars = new Dictionary<int, IBarList>();
            AccountInfo = new AccountInfo(AccountName);

            // Daily things-to-do.
            Ether.AsObservable<Heartbeat>().Where(x => x.IsHourly()).Subscribe(x =>
            {
                // At 5AM each day, we request contract details so we can see when the contracts are traded.
                foreach (var c in Contracts.Keys)
                    RequestContractDetails(c * 9999, Contracts[c].Item1);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });
        }

        private void ConnectionClosedHandler()
        {
            IsConnected = false;
        }

        public void RequestContractDetails(int RequestId, Contract Contract)
        {
            Client.ClientSocket.reqContractDetails(RequestId, Contract.IBContract);
        }

        private void PortfolioInfoHandler(UpdatePortfolioMessage obj)
        {
            throw new NotImplementedException();
        }

        private void EFPDataHandler(int arg1, int arg2, double arg3, string arg4, double arg5, int arg6, string arg7, double arg8, double arg9)
        {
            throw new NotImplementedException();
        }

        private void HistoricalDataEndHandler(HistoricalDataEndMessage obj)
        {
            throw new NotImplementedException();
        }

        private void HistoricalDataUpdateHandler(HistoricalDataMessage obj)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            if (!IsConnected)
            {
                try
                {
                    Client.ClientSocket.eConnect(Hostname, Port, ClientId);
                    Reader = new IB.EReader(Client.ClientSocket, Signal);
                    Reader.Start();

                    new Thread(() =>
                    {
                        while (Client.ClientSocket.IsConnected())
                        {
                            Signal.waitForSignal();
                            Reader.processMsgs();
                        }
                    })
                    { IsBackground = true }.Start();

                    IsConnected = Client.ClientSocket.IsConnected();
                }
                catch (Exception e)
                {
                    string errorMsg = "Please check your connection attributes";
                    ErrorHandler(-1, -1, errorMsg, e);
                }
            }
        }

        public void Subscribe(Contract contract)
        {
            if (!IsConnected)
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
                Client.ClientSocket.reqMktData(contract.Id, c.IBContract, "", false, false, new List<IB.TagValue>());
                

                // Request opening hours etc.
                // Note: this means an upper limit of 9999 contracts at any given time.
                Client.ClientSocket.reqContractDetails(contract.Id * 9999, c.IBContract);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }


        public void Unsubscribe(Contract contract)
        {
            if (IsConnected)
            {
                try
                {
                    Client.ClientSocket.cancelMktData(contract.Id);
                }
                catch (Exception e)
                {
                    ErrorHandler(-1, -1, e.ToString(), e);
                }
            }
        }


        void TickPriceHandler(TickPriceMessage message)
        {
            int contractId = message.RequestId;
            IBTickType type = (IBTickType)message.Field;
            dynamic price = message.Price;

            //Debug.WriteLine("ZEUS - TickPriceHandler: " + contractId + ", " + type + ", " + price);

            Market currentMarket = Contracts[contractId].Item2;
            currentMarket.Timestamp = Now;

            bool send = false;
            if (type == IBTickType.BidPrice && price > 0)
            {                       // TODO(live-5) - true for EFPs?
                currentMarket.Bid = price;

                if (Math.Abs(price / currentMarket.Bid - 1) < 0.15M)
                {
                    send = true;
                }
            }
            else if (type == IBTickType.AskPrice && price > 0)
            {
                currentMarket.Ask = price;

                if (Math.Abs(price / currentMarket.Ask - 1) < 0.15M)
                {
                    send = true;
                }
            }
            else if (currentMarket.Mid.Equals(decimal.MinValue) && (type == IBTickType.ClosePrice || type == IBTickType.LastPrice))
            {
                currentMarket.Ask = currentMarket.Bid = (decimal)price;
                send = true;
            }
            else if (type == IBTickType.ClosePrice)
            {
                currentMarket.PreviousClose = (decimal)price;
                send = true;
            }

            if (send)
                Ether.Send(currentMarket);
        }


        void TickSizeHandler(TickSizeMessage message)
        {
            int contractId = message.RequestId;
            IBTickType type = (IBTickType)message.Field;
            dynamic size = message.Size;

            //Debug.WriteLine("ZEUS - TickSizeHandler: " + contractId + ", " + type + ", " + size);

            Market currentMarket = Contracts[contractId].Item2;
            currentMarket.Timestamp = Now;

            bool send = false;
            if (type == IBTickType.BidSize)
            {
                currentMarket.BidSize = size;
                send = true;
            }
            else if (type == IBTickType.AskSize)
            {
                currentMarket.AskSize = size;
                send = true;
            }

            if (send)
                Ether.Send(currentMarket);
        }


        void ErrorHandler(int id, int errorCode, string str, Exception ex)
        {
            if (ex != null)
            {
                Debug.WriteLine(ex.ToString());
            } else
            {
                Debug.Write(str);
            }
        }


        void CurrentTimeHandler(long time)
        {
            StartTime = new DateTime(time);
            TimeOffset = (StartTime - DateTime.UtcNow).TotalMilliseconds;
        }


        void TWSNextValidIdHandler(object sender, Dictionary<string, object> data)
        {
            CurrentOrderId = (int)data["OrderId"];
        }


        void TickOptionHandler(MarketDataMessage message)
        {
            int contractId = message.RequestId;
            if (message is TickPriceMessage)
            {
                // do something
            }
            else if (message is TickOptionMessage)
            {
                // do something else
            }
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
        void TickGenericHandler(int id, int field, double value)
        {
            //Debug.WriteLine("ZEUS - TickGenericHandler: " + e.TickerId + ", " + Enum.GetName(typeof(TickType), e.TickType) + ", " + e.Value);
        }


        // TODO(live-3) -------------------------------------------
        void TickStringHandler(int id, int tickType, string value)
        {
            //Debug.WriteLine("ZEUS - TickStringHandler: " + e.TickerId + ", " + Enum.GetName(typeof(TickType), e.TickType) + ", " + e.Value);
        }


        void HistoricalDataHandler(HistoricalDataMessage message)
        {
            throw new NotImplementedException();
        }


        void AccountTimeHandler(UpdateAccountTimeMessage message)
        {
            // TODO(live-5)
        }


        void AccountValueHandler(AccountValueMessage message)
        {
            //string accountName = (string)data["AccountName"];
            //string key = (string)data["Key"];
            //string currency = (string)data["Currency"];
            //decimal value = (decimal)data["Value"];
            ////Debug.WriteLine("ZEUS - AccountValueHandler: {0} => {1} is worth {2} {3}", e.AccountName, e.Key, e.Currency, e.Value);

            //if (accountName == AccountInfo.AccountName)
            //{
            //    if (key == "NetLiquidation")
            //    {
            //        AccountInfo.BaseCurrency = currency;
            //        AccountInfo.NetLiquidationValue = value;
            //    }
            //    else if (key == "FullInitMarginReq")
            //    {
            //        AccountInfo.BaseCurrency = currency;
            //        AccountInfo.InitialMarginRequirement = value;
            //    }
            //    else if (key == "FullMaintMarginReq")
            //    {
            //        AccountInfo.BaseCurrency = currency;
            //        AccountInfo.MaintenanceMarginRequirement = value;
            //    }
            //    else if (key == "GrossPositionValue")
            //    {
            //        AccountInfo.BaseCurrency = currency;
            //        AccountInfo.GrossPositionValue = value;
            //    }
            //    else if (key == "Leverage-S")
            //    {                               // TODO(cleanup-5)
            //        AccountInfo.BaseCurrency = currency;
            //        AccountInfo.Leverage = value;
            //    }

            //    AccountInfo.Timestamp = Now;
            //    if (AccountInfo.IsPrimed)
            //        Ether.Send(AccountInfo);
            //}
        }


        // TODO(live-1) - figure out how to serve these. There's a MarketHours type that should be used -- Strategy already does
        // something for this.
        // One idea would be to move the DataCollator from LiveRunner to TWSSource, and then have a MarketEventDataSource that I can
        // "load" trading hours updates into. Then the DataCollator would serve the updates at the right time.
        void ContractDetailsHandler(ContractDetailsMessage message)
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