using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

using IB = IBApi;

namespace CommonTypes.BrokerUtils
{
    public delegate void ContractDetails(object sender, Dictionary<string, object> e);
    public delegate void TickPrice(object sender, Dictionary<string, object> e);
    public delegate void TickSize(object sender, Dictionary<string, object> e);
    public delegate void TickGeneric(object sender, Dictionary<string, object> e);
    public delegate void TickString(object sender, Dictionary<string, object> e);
    public delegate void TickOption(object sender, Dictionary<string, object> e);
    public delegate void HistoricalData(object sender, Dictionary<string, object> e);
    public delegate void TickEfp(object sender, Dictionary<string, object> e);
    public delegate void UpdateAccountTime(object sender, Dictionary<string, object> e);
    public delegate void UpdateAccountValue(object sender, Dictionary<string, object> e);
    public delegate void UpdatePortfolio(object sender, Dictionary<string, object> e);
    public delegate void CurrentTime(object sender, Dictionary<string, object> e);
    public delegate void Error(object sender, Dictionary<string, object> e);

    public class IBClient : IB.EWrapper
    {
        IB.EClientSocket clientSocket;
        private int nextOrderId;

        public ContractDetails ContractDetails;
        public TickPrice TickPrice;
        public TickSize TickSize;
        public TickGeneric TickGeneric;
        public TickString TickString;
        public TickOption TickOption;
        public HistoricalData HistoricalData;
        public TickEfp TickEfp;
        public UpdateAccountTime UpdateAccountTime;
        public UpdateAccountValue UpdateAccountValue;
        public UpdatePortfolio UpdatePortfolio;
        public CurrentTime CurrentTime;
        public Error Error;
        
        public IBClient()
        {
            clientSocket = new IB.EClientSocket(this);
        }

        public IB.EClientSocket ClientSocket
        {
            get { return clientSocket; }
            set { clientSocket = value; }
        }

        public int NextOrderId
        {
            get { return nextOrderId; }
            set { nextOrderId = value; }
        }

        public void Connect(string Hostname, int Port, int ClientId)
        {
            clientSocket.eConnect(Hostname, Port, ClientId);
        }

        public void RequestMarketData(int ContractId, Contract Contract, string genericTickList, bool snapshot, bool marketDataOff)
        {
            clientSocket.reqMktData(ContractId, Contract, genericTickList, snapshot);
        }

        public void CancelMarketData(int ContractId)
        {
            clientSocket.cancelMktData(ContractId);
        }

        public void RequestCurrentTime()
        {
            clientSocket.reqCurrentTime();
        }

        public void RequestContractDetails(int RequestId, Contract Contract)
        {
            clientSocket.reqContractDetails(RequestId, Contract);
        }

        public void RequestAccountUpdates(bool Subscribe, string AccountName)
        {
            clientSocket.reqAccountUpdates(Subscribe, AccountName);
        }

        public void PlaceOrder(int OrderId, Contract Contract, Order Order)
        {
            clientSocket.placeOrder(OrderId, Contract, Order);
        }

        public bool Connected
        {
            get
            {
                return clientSocket.IsConnected();
            }
        }


        public virtual void error(Exception e)
        {
            Dictionary<string, object> r = new Dictionary<string, object>();
            r["Exception"] = e;
            r["ErrorString"] = e.ToString();

            Error(this, r);
        }
        
        public virtual void error(string str)
        {
            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ErrorString"] = str;
            r["Exception"] = new Exception(str);
            Error(this, r);
        }
        
        public virtual void error(int id, int errorCode, string errorMsg)
        {
            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ErrorString"] = errorMsg;
            r["Exception"] = new Exception(errorMsg + " (Id: " + id + ", Code: " + errorCode + ")");
            r["Id"] = id;
            r["ErrorCode"] = errorCode;
            Error(this, r);
        }
        
        public virtual void connectionClosed()
        {
            Debug.WriteLine("Connection closed.\n");
        }
        
        public virtual void currentTime(long time) 
        {
            Dictionary<string, object> r = new Dictionary<string,object>();
            r["Time"] = new DateTime(time);
            CurrentTime(this, r);
        }

        public virtual void tickPrice(int tickerId, int field, double price, int canAutoExecute) 
        {
            TickType tt = (TickType)Enum.Parse(typeof(TickType), Enum.GetName(typeof(TickType), field));

            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ContractId"] = tickerId;
            r["Type"] = tt;
            r["Value"] = (decimal)price;

            TickPrice(this, r);
        }
        
        public virtual void tickSize(int tickerId, int field, int size)
        {
            TickType tt = (TickType)Enum.Parse(typeof(TickType), Enum.GetName(typeof(TickType), field));

            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ContractId"] = tickerId;
            r["Type"] = tt;
            r["Value"] = (long)size;

            TickSize(this, r);
        }
        
        public virtual void tickString(int tickerId, int tickType, string value)
        {
            TickType tt = (TickType)Enum.Parse(typeof(TickType), Enum.GetName(typeof(TickType), tickType));

            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ContractId"] = tickerId;
            r["Type"] = tt;
            r["Value"] = value;

            TickString(this, r);
        }

        public virtual void tickGeneric(int tickerId, int field, double value)
        {
            TickType tt = (TickType)Enum.Parse(typeof(TickType), Enum.GetName(typeof(TickType), field));

            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ContractId"] = tickerId;
            r["Type"] = tt;
            r["Value"] = value;

            TickGeneric(this, r);
        }

        public virtual void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture,
                                    int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            TickType tt = (TickType)Enum.Parse(typeof(TickType), Enum.GetName(typeof(TickType), tickType));

            Dictionary<string, object> r = new Dictionary<string, object>();
            r["ContractId"] = tickerId;
            r["Type"] = tt;
            r["BasisPoints"] = basisPoints;
            r["FormattedBasisPoints"] = formattedBasisPoints;
            r["ImpliedFutures"] = impliedFuture;
            r["HoldDays"] = holdDays;
            r["FutureExpiry"] = futureExpiry;
            r["DividendImpact"] = dividendImpact;
            r["DividendsToExpiry"] = dividendsToExpiry;

            TickEfp(this, r);
        }

        public virtual void tickSnapshotEnd(int tickerId)
        {
            Debug.WriteLine("TickSnapshotEnd: "+tickerId+"\n");
        }

        public virtual void nextValidId(int orderId) 
        {
            NextOrderId = orderId;
        }

        public virtual void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            Dictionary<string, object> r = new Dictionary<string, object>();
            if ((TickType)field == TickType.ModelOption)
            {
                r["ContractId"] = tickerId;
                r["OptionData"] = new OptionData((decimal)undPrice, (decimal)optPrice, impliedVolatility, delta, gamma, theta);
                TickOption(this, r);
            }
        }

        // this one sends multiple bits until accountSummaryEnd is called.
        public virtual void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            Debug.WriteLine("Acct Summary. ReqId: " + reqId + ", Acct: " + account + ", Tag: " + tag + ", Value: " + value + ", Currency: " + currency + "\n");
        }

        public virtual void updateAccountValue(string key, string value, string currency, string accountName)
        {
            Debug.WriteLine("UpdateAccountValue. Key: " + key + ", Value: " + value + ", Currency: " + currency + ", AccountName: " + accountName + "\n");
        }

        public virtual void updatePortfolio(IB.Contract contract, int position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {
            Debug.WriteLine("UpdatePortfolio. " + contract.Symbol + ", " + contract.SecType + " @ " + contract.Exchange
                + ": Position: " + position + ", MarketPrice: " + marketPrice + ", MarketValue: " + marketValue + ", AverageCost: " + averageCost
                + ", UnrealisedPNL: " + unrealisedPNL + ", RealisedPNL: " + realisedPNL + ", AccountName: " + accountName + "\n");
        }

        public virtual void updateAccountTime(string timestamp)
        {
            Debug.WriteLine("UpdateAccountTime. Time: " + timestamp + "\n");
        }

        public virtual void orderStatus(int orderId, string status, int filled, int remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            Debug.WriteLine("OrderStatus. Id: " + orderId + ", Status: " + status + ", Filled" + filled + ", Remaining: " + remaining
                + ", AvgFillPrice: " + avgFillPrice + ", PermId: " + permId + ", ParentId: " + parentId + ", LastFillPrice: " + lastFillPrice + ", ClientId: " + clientId + ", WhyHeld: " + whyHeld + "\n");
        }

        // this one sends multiple bits till openOrderEnd.
        public virtual void openOrder(int orderId, IB.Contract contract, IB.Order order, IB.OrderState orderState)
        {
            Debug.WriteLine("OpenOrder. ID: " + orderId + ", " + contract.Symbol + ", " + contract.SecType + " @ " + contract.Exchange + ": " + order.Action + ", " + order.OrderType + " " + order.TotalQuantity + ", " + orderState.Status + "\n");
            //clientSocket.reqMktData(2, contract, "", false);
            contract.ConId = 0;
            clientSocket.placeOrder(nextOrderId, contract, order);
        }

        // this one sends multiple bits till contractDetailsEnd.
        public virtual void contractDetails(int reqId, IB.ContractDetails contractDetails)
        {
            Debug.WriteLine("ContractDetails. ReqId: " + reqId + " - " + contractDetails.Summary.Symbol + ", " + contractDetails.Summary.SecType + ", ConId: " + contractDetails.Summary.ConId + " @ " + contractDetails.Summary.Exchange + "\n");

            Dictionary<string, object> rr = new Dictionary<string, object>();
            rr["Contract"] = contractDetails.Summary;
            rr["ContractDetails"] = contractDetails;

            ContractDetails(this, rr);
        }

        // this one sends multiple bits till execDetailsEnd.
        public virtual void execDetails(int reqId, IB.Contract contract, IB.Execution execution)
        {
            Debug.WriteLine("ExecDetails. " + reqId + " - " + contract.Symbol + ", " + contract.SecType + ", " + contract.Currency + " - " + execution.ExecId + ", " + execution.OrderId + ", " + execution.Shares + "\n");
        }

        // this one gets multiple
        public virtual void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            Debug.WriteLine("HistoricalData. " + reqId + " - Date: " + date + ", Open: " + open + ", High: " + high + ", Low: " + low + ", Close: " + close + ", Volume: " + volume + ", Count: " + count + ", WAP: " + WAP + ", HasGaps: " + hasGaps + "\n");
        }

        // this one gets mutliple
        public virtual void position(string account, IB.Contract contract, int pos, double avgCost)
        {
            Debug.WriteLine("Position. " + account + " - Symbol: " + contract.Symbol + ", SecType: " + contract.SecType + ", Currency: " + contract.Currency + ", Position: " + pos + ", Avg cost: " + avgCost + "\n");
        }



        public virtual void deltaNeutralValidation(int reqId, IB.UnderComp underComp)
        {
            //Debug.WriteLine("DeltaNeutralValidation. "+reqId+", ConId: "+underComp.ConId+", Delta: "+underComp.Delta+", Price: "+underComp.Price+"\n");
        }

        public virtual void managedAccounts(string accountsList) 
        {
            //Debug.WriteLine("Account list: "+accountsList+"\n");
        }

        public virtual void accountSummaryEnd(int reqId)
        {
            Debug.WriteLine("AccountSummaryEnd. Req Id: "+reqId+"\n");
        }

        public virtual void accountDownloadEnd(string account)
        {
            Debug.WriteLine("Account download finished: "+account+"\n");
        }

        public virtual void openOrderEnd()
        {
            Debug.WriteLine("OpenOrderEnd");
        }

        public virtual void contractDetailsEnd(int reqId)
        {
            Debug.WriteLine("ContractDetailsEnd. "+reqId+"\n");
        }

        public virtual void execDetailsEnd(int reqId)
        {
            Debug.WriteLine("ExecDetailsEnd. "+reqId+"\n");
        }

        public virtual void commissionReport(IB.CommissionReport commissionReport)
        {
            Debug.WriteLine("CommissionReport. "+commissionReport.ExecId+" - "+commissionReport.Commission+" "+commissionReport.Currency+" RPNL "+commissionReport.RealizedPNL+"\n");
        }

        public virtual void fundamentalData(int reqId, string data)
        {
            Debug.WriteLine("FundamentalData. " + reqId + "" + data+"\n");
        }

        public virtual void marketDataType(int reqId, int marketDataType)
        {
            Debug.WriteLine("MarketDataType. "+reqId+", Type: "+marketDataType+"\n");
        }

        public virtual void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            Debug.WriteLine("UpdateMarketDepth. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + price + ", Size" + size+"\n");
        }

        public virtual void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            Debug.WriteLine("UpdateMarketDepthL2. " + tickerId + " - Position: " + position + ", Operation: " + operation + ", Side: " + side + ", Price: " + price + ", Size" + size+"\n");
        }

        
        public virtual void updateNewsBulletin(int msgId, int msgType, String message, String origExchange)
        {
            Debug.WriteLine("News Bulletins. "+msgId+" - Type: "+msgType+", Message: "+message+", Exchange of Origin: "+origExchange+"\n");
        }

        public virtual void positionEnd()
        {
            Debug.WriteLine("PositionEnd \n");
        }

        public virtual void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            Debug.WriteLine("RealTimeBars. " + reqId + " - Time: " + time + ", Open: " + open + ", High: " + high + ", Low: " + low + ", Close: " + close + ", Volume: " + volume + ", Count: " + count + ", WAP: " + WAP+"\n");
        }

        public virtual void scannerParameters(string xml)
        {
            Debug.WriteLine("ScannerParameters. "+xml+"\n");
        }

        public virtual void scannerData(int reqId, int rank, IB.ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            Debug.WriteLine("ScannerData. "+reqId+" - Rank: "+rank+", Symbol: "+contractDetails.Summary.Symbol+", SecType: "+contractDetails.Summary.SecType+", Currency: "+contractDetails.Summary.Currency
                +", Distance: "+distance+", Benchmark: "+benchmark+", Projection: "+projection+", Legs String: "+legsStr+"\n");
        }

        public virtual void scannerDataEnd(int reqId)
        {
            Debug.WriteLine("ScannerDataEnd. "+reqId+"\n");
        }

        public virtual void receiveFA(int faDataType, string faXmlData)
        {
            Debug.WriteLine("Receing FA: "+faDataType+" - "+faXmlData+"\n");
        }

        public virtual void bondContractDetails(int requestId, IB.ContractDetails contractDetails)
        {
            Debug.WriteLine("Bond. Symbol "+contractDetails.Summary.Symbol+", "+contractDetails.Summary);
        }

        public virtual void historicalDataEnd(int reqId, string startDate, string endDate)
        {
            Debug.WriteLine("Historical data end - "+reqId+" from "+startDate+" to "+endDate);
        }
    }

    public enum TickType : int
    {
        /// <summary>
        /// Bid Size
        /// </summary>
        [Description("BID_SIZE")]
        BidSize = 0,
        /// <summary>
        /// Bid Price
        /// </summary>
        [Description("BID")]
        BidPrice = 1,
        /// <summary>
        /// Ask Price
        /// </summary>
        [Description("ASK")]
        AskPrice = 2,
        /// <summary>
        /// Ask Size
        /// </summary>
        [Description("ASK_SIZE")]
        AskSize = 3,
        /// <summary>
        /// Last Price
        /// </summary>
        [Description("LAST")]
        LastPrice = 4,
        /// <summary>
        /// Last Size
        /// </summary>
        [Description("LAST_SIZE")]
        LastSize = 5,
        /// <summary>
        /// High Price
        /// </summary>
        [Description("HIGH")]
        HighPrice = 6,
        /// <summary>
        /// Low Price
        /// </summary>
        [Description("LOW")]
        LowPrice = 7,
        /// <summary>
        /// Volume
        /// </summary>
        [Description("VOLUME")]
        Volume = 8,
        /// <summary>
        /// Close Price
        /// </summary>
        [Description("CLOSE")]
        ClosePrice = 9,
        /// <summary>
        /// Bid Option
        /// </summary>
        [Description("BID_OPTION")]
        BidOption = 10,
        /// <summary>
        /// Ask Option
        /// </summary>
        [Description("ASK_OPTION")]
        AskOption = 11,
        /// <summary>
        /// Last Option
        /// </summary>
        [Description("LAST_OPTION")]
        LastOption = 12,
        /// <summary>
        /// Model Option
        /// </summary>
        [Description("MODEL_OPTION")]
        ModelOption = 13,
        /// <summary>
        /// Open Price
        /// </summary>
        [Description("OPEN")]
        OpenPrice = 14,
        /// <summary>
        /// Low Price over last 13 weeks
        /// </summary>
        [Description("LOW_13_WEEK")]
        Low13Week = 15,
        /// <summary>
        /// High Price over last 13 weeks
        /// </summary>
        [Description("HIGH_13_WEEK")]
        High13Week = 16,
        /// <summary>
        /// Low Price over last 26 weeks
        /// </summary>
        [Description("LOW_26_WEEK")]
        Low26Week = 17,
        /// <summary>
        /// High Price over last 26 weeks
        /// </summary>
        [Description("HIGH_26_WEEK")]
        High26Week = 18,
        /// <summary>
        /// Low Price over last 52 weeks
        /// </summary>
        [Description("LOW_52_WEEK")]
        Low52Week = 19,
        /// <summary>
        /// High Price over last 52 weeks
        /// </summary>
        [Description("HIGH_52_WEEK")]
        High52Week = 20,
        /// <summary>
        /// Average Volume
        /// </summary>
        [Description("AVG_VOLUME")]
        AverageVolume = 21,
        /// <summary>
        /// Open Interest
        /// </summary>
        [Description("OPEN_INTEREST")]
        OpenInterest = 22,
        /// <summary>
        /// Option Historical Volatility
        /// </summary>
        [Description("OPTION_HISTORICAL_VOL")]
        OptionHistoricalVolatility = 23,
        /// <summary>
        /// Option Implied Volatility
        /// </summary>
        [Description("OPTION_IMPLIED_VOL")]
        OptionImpliedVolatility = 24,
        /// <summary>
        /// Option Bid Exchange
        /// </summary>
        [Description("OPTION_BID_EXCH")]
        OptionBidExchange = 25,
        /// <summary>
        /// Option Ask Exchange
        /// </summary>
        [Description("OPTION_ASK_EXCH")]
        OptionAskExchange = 26,
        /// <summary>
        /// Option Call Open Interest
        /// </summary>
        [Description("OPTION_CALL_OPEN_INTEREST")]
        OptionCallOpenInterest = 27,
        /// <summary>
        /// Option Put Open Interest
        /// </summary>
        [Description("OPTION_PUT_OPEN_INTEREST")]
        OptionPutOpenInterest = 28,
        /// <summary>
        /// Option Call Volume
        /// </summary>
        [Description("OPTION_CALL_VOLUME")]
        OptionCallVolume = 29,
        /// <summary>
        /// Option Put Volume
        /// </summary>
        [Description("OPTION_PUT_VOLUME")]
        OptionPutVolume = 30,
        /// <summary>
        /// Index Future Premium
        /// </summary>
        [Description("INDEX_FUTURE_PREMIUM")]
        IndexFuturePremium = 31,
        /// <summary>
        /// Bid Exchange
        /// </summary>
        [Description("BID_EXCH")]
        BidExchange = 32,
        /// <summary>
        /// Ask Exchange
        /// </summary>
        [Description("ASK_EXCH")]
        AskExchange = 33,
        /// <summary>
        /// Auction Volume
        /// </summary>
        [Description("AUCTION_VOLUME")]
        AuctionVolume = 34,
        /// <summary>
        /// Auction Price
        /// </summary>
        [Description("AUCTION_PRICE")]
        AuctionPrice = 35,
        /// <summary>
        /// Auction Imbalance
        /// </summary>
        [Description("AUCTION_IMBALANCE")]
        AuctionImbalance = 36,
        /// <summary>
        /// Mark Price
        /// </summary>
        [Description("MARK_PRICE")]
        MarkPrice = 37,
        /// <summary>
        /// Bid EFP Computation
        /// </summary>
        [Description("BID_EFP_COMPUTATION")]
        BidEfpComputation = 38,
        /// <summary>
        /// Ask EFP Computation
        /// </summary>
        [Description("ASK_EFP_COMPUTATION")]
        AskEfpComputation = 39,
        /// <summary>
        /// Last EFP Computation
        /// </summary>
        [Description("LAST_EFP_COMPUTATION")]
        LastEfpComputation = 40,
        /// <summary>
        /// Open EFP Computation
        /// </summary>
        [Description("OPEN_EFP_COMPUTATION")]
        OpenEfpComputation = 41,
        /// <summary>
        /// High EFP Computation
        /// </summary>
        [Description("HIGH_EFP_COMPUTATION")]
        HighEfpComputation = 42,
        /// <summary>
        /// Low EFP Computation
        /// </summary>
        [Description("LOW_EFP_COMPUTATION")]
        LowEfpComputation = 43,
        /// <summary>
        /// Close EFP Computation
        /// </summary>
        [Description("CLOSE_EFP_COMPUTATION")]
        CloseEfpComputation = 44,
        /// <summary>
        /// Last Time Stamp
        /// </summary>
        [Description("LAST_TIMESTAMP")]
        LastTimestamp = 45,
        /// <summary>
        /// Shortable
        /// </summary>
        [Description("SHORTABLE")]
        Shortable = 46,
        /// <summary>
        /// Fundamental Ratios
        /// </summary>
        [Description("FUNDAMENTAL_RATIOS")]
        FundamentalRatios = 47,
        /// <summary>
        /// Real Time Volume
        /// </summary>
        [Description("RTVOLUME")]
        RealTimeVolume = 48,
        /// <summary>
        /// When trading is halted for a contract, TWS receives a special tick: haltedLast=1. When trading is resumed, TWS receives haltedLast=0. A new tick type, HALTED, tick ID = 49, is now available in regular market data via the API to indicate this halted state.
        /// Possible values for this new tick type are:
        /// 0 = Not halted 
        /// 1 = Halted. 
        ///  </summary>
        [Description("HALTED")]
        Halted = 49,
        /// <summary>
        /// Bond Yield for Bid Price
        /// </summary>
        [Description("BID_YIELD")]
        BidYield = 50,
        /// <summary>
        /// Bond Yield for Ask Price
        /// </summary>
        [Description("ASK_YIELD")]
        AskYield = 51,
        /// <summary>
        /// Bond Yield for Last Price
        /// </summary>
        [Description("LAST_YIELD")]
        LastYield = 52,
        /// <summary>
        /// returns calculated implied volatility as a result of an calculateImpliedVolatility( ) request.
        /// </summary>
        [Description("CUST_OPTION_COMPUTATION")]
        CustOptionComputation = 53,
        /// <summary>
        /// Trades
        /// </summary>
        [Description("TRADE_COUNT")]
        TradeCount = 54,
        /// <summary>
        /// Trades per Minute
        /// </summary>
        [Description("TRADE_RATE")]
        TradeRate = 55,
        /// <summary>
        /// Volume per Minute
        /// </summary>
        [Description("VOLUME_RATE")]
        VolumeRate = 56,
        /// <summary>
        /// Last Regular Trading Hours Trade
        /// </summary>
        [Description("LAST_RTH_TRADE")]
        LastRthTrade = 57
    }
}
