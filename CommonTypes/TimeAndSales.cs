using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes
{
    public class TimeAndSales : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }

        public int ContractId { get; set; }

        public decimal FillPrice { get; set; }

        public int FillVolume { get; set; }


        public TimeAndSales(DateTimeOffset timestamp, int contractId, decimal fillprice, int fillvolume)
        {
            Timestamp = timestamp;
            ContractId = contractId;
            FillPrice = fillprice;
            FillVolume = fillvolume;
        }

        
        public override string ToString()
        {
            return string.Format("({0} at {1}: Filled {2} @ {3})", ContractId, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), FillVolume, FillPrice);
        }


        public string ToCSV()
        {
            return string.Format("{0},{1},{2},{3}", ContractId, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), FillVolume, FillPrice);
        }
    }
}
