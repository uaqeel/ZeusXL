using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes
{
    public class OrderExecution : ITimestampedDatum
    {
        public string Id;
        public DateTimeOffset Timestamp { get; set; }
        public int ContractId;
        public int OrderId;
        public int StrategyId { get; set; }

        public int FilledQuantity;                          // This is signed!
        public decimal FillPrice;

        public string Comment;

        public decimal RealisedPnL;


        public OrderExecution(string id, DateTimeOffset timestamp, int contractId, int orderId, int strategyId, int filledQuantity, decimal fillPrice, string comment)
        {
            Id = id;
            Timestamp = timestamp;
            ContractId = contractId;
            OrderId = orderId;
            StrategyId = strategyId;

            FilledQuantity = filledQuantity;
            FillPrice = fillPrice;

            Comment = comment;
        }


        public override string ToString()
        {
            return string.Format("Execution {0} @ {1}: {2} of {3} @ {4} for {5}",
                                 Id, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), FilledQuantity.ToString("+#;-#;0"),
                                 ContractId, FillPrice, StrategyId);
        }
    }
}
