using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reactive.Linq;

using ExcelDna.Integration;
using ExcelDna.Integration.Rtd;
using CommonTypes;
using CommonTypes.BrokerUtils;
using DataSources;
//using OMS;


namespace XL
{
    public class XL_RTD
    {
        private static string IBAccountId;
        private static int IBClientId;
        private static string IBHostname;
        private static int IBPort;

        public static IMessageBus Ether;
        public static HeartbeatDataSource HeartbeatSource;
        public static TWSSource DataSource;
        //public static TWSOMS OMS;


        static XL_RTD()
        {
            IBAccountId = XL.Config.AppSettings.Settings["IBAccountId"].Value;
            IBClientId = Int32.Parse(XL.Config.AppSettings.Settings["IBClientId"].Value);
            IBHostname = XL.Config.AppSettings.Settings["IBHostname"].Value;
            IBPort = Int32.Parse(XL.Config.AppSettings.Settings["IBPort"].Value);

            Ether = new Ether(XL.EpochSecs);
            HeartbeatSource = new HeartbeatDataSource(DateTimeOffset.UtcNow, DateTimeOffset.MaxValue, XL.EpochSecs, "Epoch Heartbeat");
            DataSource = new TWSSource(Ether, IBHostname, IBPort, IBClientId, IBAccountId, false, 86400000);
            //OMS = new TWSOMS(Ether, IBHostname, IBPort, IBClientId + 1);

            dynamic excel = ExcelDnaUtil.Application;
            excel.RTD.ThrottleInterval = 500;
        }


        [ExcelFunction(Category = "XL_RTD", Description = "Returns a Contract's current market by RTD")]
        public static object GetMarket(string Contract, string TickType)
        {
            try
            {
                // Check to make sure it's a valid tick type...
                Enum.Parse(typeof(IBTickType), TickType);

                Contract cc = XLOM.Get<Contract>(Contract);

                object x = "Error, no contract with key '" + Contract + "'!";
                if (cc != null)
                    x = XlCall.RTD("XL.TickServer", null, Contract, TickType);

                return x;
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine("GetMarket: " + ae.ToString());
                return "Error: Couldn't parse tick type. " + ae.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetMarket: " + e.ToString());
                return e.ToString();
            }
        }


        [ExcelFunction(Category = "XL_RTD", Description = "Demo function that returns current time via RTD")]
        public static object RTDDemo()
        {
            return XlCall.RTD("XL.TimeServer", null, "NOW");
        }
    }


    public class TickServer : IRtdServer
    {
        private Dictionary<int, int> TopicsToContractIds = new Dictionary<int,int>();
        private Dictionary<int, IBTickType> TopicsToTickTypes = new Dictionary<int,IBTickType>();
        private Dictionary<int, Market> ContractsToMarkets = new Dictionary<int, Market>();
        private Dictionary<int, bool> ContractsUpdated = new Dictionary<int, bool>();
        private IRTDUpdateEvent RTDCallback;


        #region IRtdServer Members
        public int ServerStart(IRTDUpdateEvent CallbackObject)
        {
            RTDCallback = CallbackObject;

            return 1;
        }

        public void ServerTerminate()
        {
            RTDCallback = null;

            TopicsToContractIds.Clear();
            TopicsToTickTypes.Clear();
            ContractsToMarkets.Clear();
        }

        public object ConnectData(int TopicId, ref Array Strings, ref bool GetNewValues)
        {
            if (Strings.Length != 2)
            {
                throw new Exception("Error: Must supply ContractId and TickType!");
            }

            string contractKey = Strings.GetValue(0) as string;                                                             // The key in OM.
            Contract cc = XLOM.Get<Contract>(contractKey);
            int contractId = cc.Id;
            string contractCcy = cc.Currency;

            IBTickType tickType = (IBTickType)Enum.Parse(typeof(IBTickType), Strings.GetValue(1) as string, true);

            // If we're not already subscribed to this contract, crank up TWSSource.
            if (!(TopicsToContractIds.ContainsKey(TopicId) && TopicsToTickTypes.ContainsKey(TopicId))) {
                TopicsToContractIds.Add(TopicId, contractId);
                TopicsToTickTypes.Add(TopicId, tickType);

                if (!ContractsToMarkets.ContainsKey(contractId))
                {
                    ContractsToMarkets.Add(contractId, new Market(DateTimeOffset.UtcNow, contractId, -1, -1));
                    ContractsUpdated.Add(contractId, true);

                    XL_RTD.DataSource.Subscribe(cc);
                    XL_RTD.Ether.AsObservable<Market>().Where(x => x.ContractId == contractId).Subscribe(x =>
                    {
                        ContractsToMarkets[x.ContractId] = x;
                        ContractsUpdated[x.ContractId] = true;

                        try
                        {
                            RTDCallback.UpdateNotify();
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.StackTrace);
                        }
                    },
                    e =>
                    {
                        Debug.WriteLine(e.ToString());
                    });
                }
            }

            return "(WAITING)";
        }

        public void DisconnectData(int TopicId)
        {
            int contractId = TopicsToContractIds[TopicId];

            TopicsToContractIds.Remove(TopicId);
            TopicsToTickTypes.Remove(TopicId);

            if (TopicsToContractIds.Where(x => x.Value == contractId).Count() == 0)
            {
                XL_RTD.DataSource.Unsubscribe(XLOM.GetById<Contract>(contractId));
            }
        }

        public int Heartbeat()
        {
            return 1;
        }

        public Array RefreshData(ref int TopicCount)
        {
            object lockObject = new object();
            List<int> updatedTopics = null;

            lock (lockObject)
            {
                IEnumerable<int> updatedContracts = from u in ContractsUpdated
                                                    where u.Value == true
                                                    select u.Key;

                updatedTopics = (from t in TopicsToContractIds
                                 where updatedContracts.Contains(t.Value)
                                 select t.Key).ToList();

            }

            TopicCount = updatedTopics.Count();


            object[,] results = new object[2, TopicCount];
            for (int i = 0; i < TopicCount; ++i)
            {
                results[0, i] = updatedTopics[i];

                if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.BidSize)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].BidSize;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.BidPrice)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].Bid;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.AskPrice)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].Ask;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.AskSize)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].AskSize;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.MidPrice)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].Mid;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.LastPrice)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].Micro;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.ClosePrice)
                    results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].PreviousClose;
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.ImpliedVol)
                {
                    if (ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData == null)
                        results[1, i] = 0;
                    else
                        results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData.ImpliedVol;
                }
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.Delta)
                {
                    if (ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData == null)
                        results[1, i] = 0;
                    else
                        results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData.Delta;
                }
                else if (TopicsToTickTypes[updatedTopics[i]] == IBTickType.Theta)
                {
                    if (ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData == null)
                        results[1, i] = 0;
                    else
                        results[1, i] = ContractsToMarkets[TopicsToContractIds[updatedTopics[i]]].OptionData.Theta;
                }
            }

            return results;
        }
        #endregion
    }


    public class TimeServer : IRtdServer
    {
        private IRTDUpdateEvent _callback;
        private Timer _timer;
        private int _topicId;

        #region IRtdServer Members
        public object ConnectData(int topicId, ref Array Strings, ref bool GetNewValues)
        {
            _topicId = topicId;
            _timer.Start();
            return GetTime();
        }

        public void DisconnectData(int topicId)
        {
            _timer.Stop();
        }

        public int Heartbeat()
        {
            return 1;
        }

        public Array RefreshData(ref int topicCount)
        {
            object[,] results = new object[2, 1];
            results[0, 0] = _topicId;
            results[1, 0] = GetTime();

            topicCount = 1;
            _timer.Start();

            return results;
        }

        public int ServerStart(IRTDUpdateEvent CallbackObject)
        {
            _callback = CallbackObject;
            _timer = new Timer();
            _timer.Tick += Callback;
            _timer.Interval = 500;
            return 1;
        }

        public void ServerTerminate()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        #endregion

        private void Callback(object sender, EventArgs e)
        {
            _timer.Stop();
            _callback.UpdateNotify();
        }

        private string GetTime()
        {
            return DateTime.UtcNow.ToString("HH:mm:ss.fff");
        }
    }
}
