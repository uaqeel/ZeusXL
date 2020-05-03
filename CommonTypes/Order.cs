using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

using IB = IBApi;


namespace CommonTypes
{
    [DataContract]
    public class Order : IB.Order, IDisposable, ITimestampedDatum
    {
        public int Id
        {
            get
            {
                return OrderId;
            }
            private set
            {
                OrderId = value;
            }
        }

        new public PositiveInteger TotalQuantity
        {
            get
            {
                return new PositiveInteger(base.TotalQuantity);                   // Absolute.
            }
            set
            {
                base.TotalQuantity = value.Value;
            }
        }

        new public TradeAction Action
        {
            get
            {
                return (TradeAction)Enum.Parse(typeof(TradeAction), base.Action);
            }
            set
            {
                base.Action = Enum.GetName(typeof(TradeAction), value);
            }
        }

        new public OrderType OrderType
        {
            get
            {
                return (OrderType)Enum.Parse(typeof(OrderType), base.OrderType);
            }
            set
            {
                base.OrderType = Enum.GetName(typeof(OrderType), value);
            }
        }

        public TimeInForce TimeInForce {
            get {
                return (TimeInForce)Enum.Parse(typeof(TimeInForce), Tif);
            } set {
                Tif = Enum.GetName(typeof(TimeInForce), value);
            }
        }

        public DateTimeOffset Timestamp { get; set; }
        public int ContractId;
        public decimal DesiredPrice;
        public Purpose Purpose;
        public int StrategyId;
        public string Comment;                                  // This is a purely optional field.


        public Order(DateTimeOffset timestamp, int contractId, int strategyId, TradeAction action, PositiveInteger quantity,
                     OrderType orderType, TimeInForce timeInForce, decimal desiredPrice)
        {
            // Orders are assigned an Id by the OMS, so until then, they have Id == -1.
            Id = -1;

            Timestamp = timestamp;
            ContractId = contractId;
            StrategyId = strategyId;

            Action = action;
            TotalQuantity = quantity;

            OrderType = orderType;
            TimeInForce = timeInForce;

            DesiredPrice = desiredPrice;

            Comment = "";
            Purpose = CommonTypes.Purpose.Unspecified;
        }


        public Order(Order rhs) : this(rhs.Timestamp, rhs.ContractId, rhs.StrategyId,
                                       rhs.Action, rhs.TotalQuantity, rhs.OrderType, rhs.TimeInForce, rhs.DesiredPrice)
        {
            Comment = rhs.Comment;
            Purpose = rhs.Purpose;
        }


        public override string ToString()
        {
            return string.Format("Order {0} @ {1}: {2} of {3} @ {4} for {5}",
                                 Id, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), ((int)Action * TotalQuantity.Value).ToString("+#;-#;0"),
                                 ContractId, DesiredPrice, StrategyId);
        }

        public override bool Equals(Object p_other)
        {
            return base.Equals(p_other);
        }


        // TODO(1) - not happy with this yet.
        /// <summary>
        /// This merges orders based on a weighted average. NOTE.
        /// </summary>
        /// <param name="theoriginal"></param>
        /// <param name="thenewone"></param>
        /// <returns></returns>
        public virtual void Merge(Order theoriginal)
        {
            if (theoriginal != null && theoriginal.Action != TradeAction.Wait)
            {
                if (ContractId != theoriginal.ContractId)
                    throw new Exception("Error, can't merge orders if they're on different contracts!");

                if (OrderType != theoriginal.OrderType)
                    throw new Exception("Error, can't merge orders of different OrderTypes!");

                if (TimeInForce != theoriginal.TimeInForce)
                    throw new Exception("Error, can't merge orders with different TimeInForces!");

                if (OrderType != CommonTypes.OrderType.Market)
                    throw new Exception("Error, order merging currently only available for Market orders!");

                int stopLossQuantity = (theoriginal.TotalQuantity.Value * (int)theoriginal.Action) + (TotalQuantity.Value * (int)Action);
                TotalQuantity = (PositiveInteger)stopLossQuantity;
                Action = (TradeAction)Math.Sign(stopLossQuantity);
                
                // Example: buy 5 (stop: sell 5) THEN sell 1 (stop: buy 1) = long 4 (stop: sell 4). On the other hand,
                // buy 1 (stop: sell 1) THEN sell 1 (stop: buy 1) = long 0 (stop: null).
                if (Action == TradeAction.Wait)
                    return;

                if (theoriginal.Action != Action)
                {
                    // Example: original was sell 2 @ (x - eps) and new one was buy 1 @ (x + eps), where x is the market price.
                    // Then the correct thing to do is to sell 1 @ (x - eps).
                    DesiredPrice = (Action == theoriginal.Action ? theoriginal.DesiredPrice : DesiredPrice);
                }
                else
                {
                    // If you're scaling into a position and merge stop-loss orders as you enter, doing a restrictive merge of stop-losses 
                    // results in the per-unit-loss at the new stop-loss level being higher than the original per-unit-loss. For strategies
                    // that double-up on the way down, this means being forced out of positions earlier than they otherwise would as
                    // the market goes against them (since they will be stopped out at the original stop-loss).
                    //
                    // On the flip side, if the position is being built on the way up and then the market reverses to the exit price, the
                    // extra loss taken due to the exit price being a weighted average rather than the tighter exit is
                    //     0.5 * (theoriginal.TotalQuantity + thenewone.TotalQuantity) * (theoriginal.DesiredPrice - thenewone.DesiredPrice)
                    DesiredPrice = (theoriginal.TotalQuantity.Value * theoriginal.DesiredPrice + TotalQuantity.Value * DesiredPrice) /
                                    (theoriginal.TotalQuantity + TotalQuantity).Value;
                }
            }
        }


        // Quantity is absolute.
        public static Order MarketOrder(DateTimeOffset Timestamp, TradeAction Action, int ContractId, int StrategyId, PositiveInteger Quantity, decimal DesiredPrice)
        {
            Order o = new Order(Timestamp, ContractId, StrategyId, Action, Quantity, OrderType.Market, TimeInForce.Day, DesiredPrice);

            return o;
        }


        public static Order MarketOrder(Market CurrentMarket, TradeAction Action, int StrategyId, PositiveInteger Quantity)
        {
            return MarketOrder(CurrentMarket.Timestamp, Action, CurrentMarket.ContractId, StrategyId, Quantity,
                               (Action == CommonTypes.TradeAction.Buy ? CurrentMarket.Ask : CurrentMarket.Bid));
        }


        public static Tuple<Order, StopLossOrder, TakeProfitOrder> OrderTriple(Market CurrentMarket, TradeAction Action, int ContractId, int StrategyId, PositiveInteger Quantity,
                                                     decimal TakeProfit, decimal StopLoss, bool TrailingStopLoss)
        {
            decimal desiredPrice = (Action == TradeAction.Buy ? CurrentMarket.Ask : CurrentMarket.Bid);
            Order entry = MarketOrder(CurrentMarket.Timestamp, Action, ContractId, StrategyId, Quantity, desiredPrice);
            StopLossOrder stoploss = new StopLossOrder(entry, StopLoss, TrailingStopLoss);
            TakeProfitOrder takeprofit = new TakeProfitOrder(entry, TakeProfit);

            return new Tuple<Order, StopLossOrder, TakeProfitOrder>(entry, stoploss, takeprofit);
        }


        public static Order TestExitOrders(Market CurrentMarket, StopLossOrder StopLossOrder, TakeProfitOrder TakeProfitOrder)
        {
            if (StopLossOrder != null && StopLossOrder.Test(CurrentMarket))
                return StopLossOrder;

            if (TakeProfitOrder != null && TakeProfitOrder.Test(CurrentMarket))
                return TakeProfitOrder;

            return null;
        }


        // This is so that Orders can be used in using statements.
        public void Dispose()
        {
        }
    }


    [DataContract]
    public class StopLossOrder : Order
    {
        [DataMember]
        public decimal StopLoss;                                                                // In dollars, the distance from the entry.

        [DataMember]
        public bool TrailFlag;


        public StopLossOrder(Order EntryOrder, decimal StopLossInDollars, bool Trail)
            : base(EntryOrder)
        {
            Purpose = Purpose.StopLoss;
            StopLoss = StopLossInDollars;
            TrailFlag = Trail;

            if (EntryOrder != null)
            {
                if (EntryOrder.GetType() == typeof(TakeProfitOrder))
                    Action = EntryOrder.Action;                                                 // EntryOrder is actually a take-profit that we're converting to a trailing stop-loss.
                else
                    Action = (TradeAction)(-(int)EntryOrder.Action);                                 // EntryOrder is an actual entry order that we need a matching stop-loss for.

                DesiredPrice = (int)Action * StopLoss + EntryOrder.DesiredPrice;
            }
            else
            {
                throw new Exception("Error, null entry order!");
            }
        }


        public bool Test(Market CurrentMarket)
        {
            decimal executablePrice = (Action == TradeAction.Buy ? CurrentMarket.Ask : CurrentMarket.Bid);

            // We want to buy, action == 1, do it if executablePrice > stopprice
            // We want to sell, action == -1, do it if executablePrice < stopprice
            if (Math.Sign(executablePrice - DesiredPrice) == Math.Sign((int)Action))
            {
                Timestamp = CurrentMarket.Timestamp;                                  // CRUCIAL!
                return true;
            }
            else
            {
                Update(CurrentMarket);
            }

            return false;
        }


        public void Update(Market CurrentMarket)
        {
            if (TrailFlag)
            {
                decimal executablePrice = (Action == TradeAction.Buy ? CurrentMarket.Ask : CurrentMarket.Bid);
                decimal possibleUpdatePrice = executablePrice + (int)Action * StopLoss;

                if (possibleUpdatePrice != DesiredPrice &&
                    Math.Sign(possibleUpdatePrice - executablePrice) != Math.Sign(possibleUpdatePrice - DesiredPrice))
                {
                    DesiredPrice = possibleUpdatePrice;

                    if (Purpose == Purpose.StopLoss)
                        Purpose = Purpose.TrailingStopLoss;
                }
            }
        }


        public override void Merge(Order theoriginal)
        {
            if (theoriginal != null && theoriginal.Action != TradeAction.Wait)
            {
                if (ContractId != theoriginal.ContractId)
                    throw new Exception("Error, can't merge orders if they're on different contracts!");

                if (OrderType != theoriginal.OrderType)
                    throw new Exception("Error, can't merge orders of different OrderTypes!");

                if (TimeInForce != theoriginal.TimeInForce)
                    throw new Exception("Error, can't merge orders with different TimeInForces!");

                if (OrderType != CommonTypes.OrderType.Market)
                    throw new Exception("Error, order merging currently only available for Market orders!");

                int stopLossQuantity = (theoriginal.TotalQuantity.Value * (int)theoriginal.Action) + (TotalQuantity.Value * (int)Action);
                Action = (TradeAction)Math.Sign(stopLossQuantity);

                // Example: buy 5 (stop: sell 5) THEN sell 1 (stop: buy 1) = long 4 (stop: sell 4). On the other hand,
                // buy 1 (stop: sell 1) THEN sell 1 (stop: buy 1) = long 0 (stop: null).
                if (Action == TradeAction.Wait)
                    return;

                if (theoriginal.Action != Action)
                {
                    // Example: original was sell 2 @ (x - eps) and new one was buy 1 @ (x + eps), where x is the market price.
                    // Then the correct thing to do is to sell 1 @ (x - eps).
                    DesiredPrice = (Action == theoriginal.Action ? theoriginal.DesiredPrice : DesiredPrice);
                }
                else
                {
                    // If you're scaling into a position and merge stop-loss orders as you enter, doing a restrictive merge of stop-losses 
                    // results in the per-unit-loss at the new stop-loss level being higher than the original per-unit-loss. For strategies
                    // that double-up on the way down, this means being forced out of positions earlier than they otherwise would as
                    // the market goes against them (since they will be stopped out at the original stop-loss).
                    //
                    // On the flip side, if the position is being built on the way up and then the market reverses to the exit price, the
                    // extra loss taken due to the exit price being a weighted average rather than the tighter exit is
                    //     0.5 * (theoriginal.TotalQuantity + thenewone.TotalQuantity) * (theoriginal.DesiredPrice - thenewone.DesiredPrice)
                    //DesiredPrice = (theoriginal.TotalQuantity.Value * theoriginal.DesiredPrice + TotalQuantity.Value * DesiredPrice) /
                    //                (theoriginal.TotalQuantity + TotalQuantity).Value;
                    if (Action == CommonTypes.TradeAction.Sell)
                        DesiredPrice = Math.Max(theoriginal.DesiredPrice, DesiredPrice + StopLoss - StopLoss / ((decimal)Math.Abs(stopLossQuantity) / TotalQuantity.Value));
                    else if (Action == CommonTypes.TradeAction.Buy)
                        DesiredPrice = Math.Min(theoriginal.DesiredPrice, DesiredPrice - StopLoss + StopLoss / ((decimal)Math.Abs(stopLossQuantity) / TotalQuantity.Value));
                }

                TotalQuantity = (PositiveInteger)stopLossQuantity;
            }
        }
    }


    [DataContract]
    public class TakeProfitOrder : Order
    {
        [DataMember]
        public decimal TakeProfit;                                                                // In dollars, the distance from the entry.

        private StopLossOrder TrailingTakeProfit;


        public TakeProfitOrder(Order EntryOrder, decimal TakeProfitInDollars)
            : base(EntryOrder)
        {
            Purpose = Purpose.TakeProfit;
            TakeProfit = TakeProfitInDollars;

            if (EntryOrder != null)
            {
                Action = (TradeAction)(-(int)EntryOrder.Action);

                decimal entryPrice = EntryOrder.DesiredPrice;
                DesiredPrice = (int)EntryOrder.Action * TakeProfit + entryPrice;
            }
            else
            {
                throw new Exception("Error, null entry order!");
            }
        }


        public bool Test(Market CurrentMarket)
        {
            decimal executablePrice = (Action == TradeAction.Buy ? CurrentMarket.Ask : CurrentMarket.Bid);

            if (Purpose == Purpose.TakeProfit)
            {
                // We want to buy (action == 1) do it if executablePrice < targetprice
                // We want to sell (action == -1) do it if executablePrice > targetprice
                if (Math.Sign(DesiredPrice - executablePrice) == Math.Sign((int)Action))
                {
                    Timestamp = CurrentMarket.Timestamp;                                  // CRUCIAL!
                    Purpose = Purpose.TrailingTakeProfit;

                    // We've hit the original take-profit level, so now we switch to being a trailing
                    // take-profit (ie, a very tight stop-loss).
                    TrailingTakeProfit = new StopLossOrder(this, 10 * CurrentMarket.Spread, true);
                    TrailingTakeProfit.Action = Action;
                }
            }
            else
            {
                if (TrailingTakeProfit.Test(CurrentMarket))
                    return true;
                else
                    TrailingTakeProfit.Update(CurrentMarket);
            }

            return false;
        }
    }


    public enum Purpose
    {
        Unspecified,
        Entry,
        Exit,
        StopLoss,
        TrailingStopLoss,
        TakeProfit,
        TrailingTakeProfit,
        Timeout,
        Closeout,
        Rebalancing
    }


    public enum TradeAction : int
    {
        Buy = +1,
        Sell = -1,
        Wait = 0
    }


    public enum OrderType
    {
        Market,
        Limit,
        Stop
    }


    public enum TimeInForce
    {
        Day,
        GoodTillCancelled,
        ImmediateOrCancel,
        FillOrKill,
        GoodTillDate,
        MarketOnOpen
    }
}
