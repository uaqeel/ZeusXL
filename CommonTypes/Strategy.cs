using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Reactive.Linq;


// This class maintains the following for convenience:
//     - a dictionary of stop-loss orders and one of take-profit orders -- these are assigned to by the derived strategy, but are monitored automatically.
//     - a dictionary of the net positions in each contract for this strategy -- these are updated automatically.
//     - variables that contain the initial and current account value -- these are initialised automatically and the latter is updated automatically via PnLInfos.
//     - a dictionary containing market operating hours for each contract -- these are initialised and updated automatically. 
namespace CommonTypes
{
    public abstract class Strategy : IIdentifiable
    {
        public int Id { get; private set; }

        public SerialisableDictionary Config;

        public Dictionary<int, Contract> Contracts;

        public Dictionary<int, StopLossOrder> StopLossOrders;                   // Up to one stop-loss per contract. Will be monitored automatically if present.

        public Dictionary<int, TakeProfitOrder> TakeProfitOrders;               // Up to one take-profit per contract. Will be monitored automatically if present.

        public Dictionary<int, int> NetQuantity;                                // The current position in each contract. Will be updated automatically.

        public Dictionary<int, MarketHours> MarketHours;                        // The current market operating hours for each contract. Updated automatically.

        public string BaseCurrency;

        public decimal InitialAccountValue;                                     // Initial dollar value of the account in the BaseCurrency.

        public decimal CurrentAccountValue;                                     // Current dollar values of the account in the BaseCurrency. Will be updated automatically.

        protected IMessageBus Ether;

        protected PositionSizer Sizer;


        public Strategy(int id, Dictionary<int, Contract> contracts, SerialisableDictionary config)
        {
            Id = id;
            Contracts = contracts;
            Config = new SerialisableDictionary(config);
        }
        

        // All instance variables MUST be initialised by calling this function. It wraps InitialiseStrategy
        // to provide default behaviour.
        // TODO(3) - would it be a better idea to just reconstruct the strategies each time (now that we have
        // the Config saved)? There's always a chance an instance variable isn't initialised correctly this way...
        public void Initialise(IMessageBus Ether, string StrategyBaseCurrency, decimal DollarAccountValue)
        {
            this.Ether = Ether;

            StopLossOrders = new Dictionary<int, StopLossOrder>();
            TakeProfitOrders = new Dictionary<int, TakeProfitOrder>();

            NetQuantity = new Dictionary<int, int>();
            foreach (int cId in Contracts.Keys)
            {
                NetQuantity[cId] = 0;
                StopLossOrders[cId] = null;
                TakeProfitOrders[cId] = null;
            }

            BaseCurrency = StrategyBaseCurrency;
            InitialAccountValue = DollarAccountValue;
            CurrentAccountValue = DollarAccountValue;

            // Initialise this strategy's positiong sizer.
            Sizer = new PositionSizer(Config, DollarAccountValue);

            // Initialise the derived strategy. Be sure to do this before adding the basic subscriptions
            // to ensure that any instance variables that require subscriptions (eg, BarLists) get
            // ticks before the basic subscription handlers do.
            InitialiseStrategy(Ether);

            // Now set up the basic Ether subscriptions.
            InitialiseBasicSubscriptions(Ether);
        }


        private void InitialiseBasicSubscriptions(IMessageBus Ether)
        {
            // Inform PMS and any components that we exist!
            Ether.Send(new StrategyInfo(Id));

            // Subscribe to relevant ticks, check exit orders and then pass on to the derived strategy.
            Ether.AsObservable<Market>().Where(x => Contracts.Keys.Contains(x.ContractId)).Subscribe(x =>
            {
                CheckAndSendExitOrders(x, NetQuantity[x.ContractId]);

                OnTick(x);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });

            Ether.AsObservable<Bar>().Where(x => Contracts.Keys.Contains(x.ContractId)).Subscribe(x =>
            {
                CheckAndSendExitOrders(x.LastMarket, NetQuantity[x.ContractId]);

                OnBar(x);
                OnTick(x.LastMarket);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });

            // Subscribe to relevant order executions, update NetQuantity and then pass on to the
            // derived strategy.
            Ether.AsObservable<OrderExecution>().Where(x => x.StrategyId == Id).Subscribe(x =>
            {
                NetQuantity[x.ContractId] += x.FilledQuantity;
                if (NetQuantity[x.ContractId] == 0)
                {
                    StopLossOrders[x.ContractId] = null;
                    TakeProfitOrders[x.ContractId] = null;
                }

                // Send the position sizer execution info.
                Sizer.ProcessOrderExecution(x);

                OnExecution(x);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });


            // Subscribe to PnL infos, update account value and then pass on to the derived strategy.
            Ether.AsObservable<PnLInfo>().Where(x => x.StrategyId == Id).Subscribe(x =>
            {
                CurrentAccountValue = x.CurrentAccountValue;

                // Send the position sizer PnL info.
                Sizer.ProcessPnLInfo(x);

                OnPnL(x);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });


            // Subscribe to updates of the trading hours for any contracts and update MarketHours.
            Ether.AsObservable<MarketHours>().Where(x => Contracts.Keys.Contains(x.ContractId)).Subscribe(x =>
            {
                MarketHours[x.ContractId] = x;
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
            });
        }

        
        // *All* state variables should be initialised in this function -- called by Backtester
        // each time we run a simulation. Since this is where strategies are attached to the
        // active message bus, it's also where they should subscribe to data, executions, etc.
        protected abstract void InitialiseStrategy(IMessageBus Ether);


        // These are called from Strategy's basic Ether subscriptions. NOTE: this means you should
        // not be subscribing to the markets elsewhere in your strategies!
        protected abstract void OnTick(Market x);
        protected abstract void OnBar(Bar x);
        protected abstract void OnExecution(OrderExecution x);
        protected abstract void OnPnL(PnLInfo x);


        public bool CheckAndSendExitOrders(Market mkt, int netQuantity)
        {
            using (Order exit = Order.TestExitOrders(mkt, StopLossOrders.GetOrDefault(mkt.ContractId, default(StopLossOrder)),
                                                          TakeProfitOrders.GetOrDefault(mkt.ContractId, default(TakeProfitOrder))))
            {
                if (exit != null && netQuantity != 0)
                {
                    // Make sure we never have a stale exit order lying around that gets executed because it wasn't cleaned up...
                    if (Math.Sign(netQuantity) != -(int)exit.Action)
                        throw new Exception("Strategy::CheckAndSendExitOrders -- error, invalid exit order!");

                    exit.TotalQuantity = (PositiveInteger)netQuantity;
                    Ether.Send(exit);

                    StopLossOrders[mkt.ContractId] = null;
                    TakeProfitOrders[mkt.ContractId] = null;

                    return true;
                }
            }

            return false;
        }


        public static Strategy Create(string strategyType, int strategyId, Dictionary<int, Contract> contracts, Dictionary<string, object> config, string assemblyOpt)
        {
            // Load the assembly in which this strategy lives...
            if (assemblyOpt.Equals(string.Empty))
                assemblyOpt = "Strategies";

            // This will throw...
            System.AppDomain.CurrentDomain.Load(assemblyOpt);

            //...and then create an instance of it, calling the constructor that takes a dictionary
            // for initialisation.
            Type type = Utils.FindType(strategyType, "Strategies");
            Strategy s = Activator.CreateInstance(type, new object[] { strategyId, contracts, config }) as Strategy;
            return s;
        }


        public static Strategy Create(int strategyId, string text, Dictionary<int, Contract> allContracts, string assemblyOpt)
        {
            // Load the assembly in which this strategy lives...
            if (assemblyOpt.Equals(string.Empty))
                assemblyOpt = "Strategies";

            // This will throw...
            System.AppDomain.CurrentDomain.Load(assemblyOpt);

            string[] lines = text.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

            Type type = Utils.FindType(lines[0].Split(':').Last(), "");

            var contractIds = lines[1].Split(':').Last().Split(',');
            Dictionary<int, Contract> contracts = allContracts.Where(x => contractIds.Contains(x.Value.Symbol)).ToDictionary(x => x.Key, x => x.Value);
            if (contracts.Count == 0)
                throw new Exception("Error creating strategy: specified contracts not supplied!");

            Dictionary<string, object> config = new Dictionary<string, object>();
            for (int i = 2; i < lines.Length; ++i)
            {
                string[] line = lines[i].Split(',');
                if (line[1] == typeof(string).ToString())
                    config.Add(line[0], line[2]);
                else if (line[1] == typeof(int).ToString())
                    config.Add(line[0], int.Parse(line[2]));
                else if (line[1] == typeof(double).ToString())
                    config.Add(line[0], double.Parse(line[2]));
                else if (line[1] == typeof(DateTimeOffset).ToString())
                    config.Add(line[0], DateTimeOffset.Parse(line[2]));
            }

            Strategy s = Activator.CreateInstance(type, new object[] { strategyId, contracts, config }) as Strategy;
            return s;
        }


        // NOTE: This class is deliberately non-serializable. Use this method and the Create method in tandem.
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Type:" + GetType());
            sb.AppendLine("ContractIds:" + string.Join(",", Contracts.Values.Select(x => x.Symbol)));
            sb.AppendLine(Config.ToString());

            return sb.ToString();
        }
    }
}
