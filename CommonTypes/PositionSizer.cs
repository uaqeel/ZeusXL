using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTypes;
using CommonTypes.Maths;


namespace CommonTypes
{
    // Provides a well-encapsulated way for strategies to size positions.
    //
    // This is much lighter-weight than the TAA strategies in that it only seeks to size the positions, not
    // fundamentally alter them, as, for example, OBPI can do if a straddle is being written on a strategy.
    // So, the types of sizing strategies this is suitable for are:
    // - constant size
    // - Kelly betting (betting your edge)
    // - constant proportion of wealth
    // - martingale betting
    // - BS delta betting - without rebalancing of existing positions
    //
    // Note that the sizing algorithms currently implemented assume the same sizing rules apply
    // for each contract in a multi-contract strategy. More complicated rules can easily be created.
    //
    // Note that sizing can generally be overlaid on top of strategies -- this means that a strategy could
    // be backtested with a simple constant-size sizing algorithm and then have different sizers overlaid
    // after the backtest for a quick appraisal of the different sizing algorithms.
    //
    // http://edgesense.net/wp-content/uploads/2012/08/allocation-algos.pdf
    public class PositionSizer
    {
        private IPositionSizingLogic Logic;


        public PositionSizer(Dictionary<string, object> config, decimal initialAccountSize)
        {
            string typeName = config.GetOrDefault("SizingAlgorithm", "ConstantSize").ToString();
            Type type = Utils.FindType(typeName, "");

            Logic = Activator.CreateInstance(type, new object[] { config, initialAccountSize}) as IPositionSizingLogic;
        }


        public void ProcessPnLInfo(PnLInfo x)
        {
            Logic.ProcessPnLInfo(x);
        }


        public void ProcessOrderExecution(OrderExecution x)
        {
            Logic.ProcessOrderExecution(x);
        }


        public Tuple<TradeAction, PositiveInteger> GetQuantityToTrade(Contract contract, Market currentMarket, int currentPosition, TradeAction action)
        {
            // Prevent orphaned positions.
            if (Math.Sign(currentPosition) == -(int)action)
            {
                return new Tuple<TradeAction, PositiveInteger>(action, new PositiveInteger(currentPosition));
            }

            return Logic.GetQuantityToTrade(contract, currentMarket, currentPosition, (int)action);
        }
    }


    public interface IPositionSizingLogic
    {
        void ProcessPnLInfo(PnLInfo x);
        void ProcessOrderExecution(OrderExecution x);
        Tuple<TradeAction, PositiveInteger> GetQuantityToTrade(Contract contract, Market currentMarket, int currentPosition, int action);
    }
}
