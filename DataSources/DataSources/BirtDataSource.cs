using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Reactive;

using CommonTypes;
using DataSources;


namespace DataSources
{
    public class BirtDataSource : GenericFileBasedDataSource<BirtDukasDataReader>
    {
        public BirtDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public BirtDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }

    
    public class BirtDukasDataReader : IDataReader
    {
        int ContractId;
        Market[] Markets;


        public void Initialise(int contractId, string filename)
        {
            ContractId = contractId;
            Markets = ReadFile(filename);
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            foreach (Market m in Markets)
            {
                yield return m;
            }
        }


        private Market[] ReadFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            if (lines.Length == 0)
                return new Market[0];

            Market[] markets = new Market[lines.Length - 1];
            decimal lastMid = decimal.Zero;
            for (int i = 0; i < markets.Length; ++i)
            {
                string[] tokens = lines[i].Split(',');

                DateTimeOffset timestamp = DateTimeOffset.Parse(tokens[0]);
                decimal bid = decimal.Parse(tokens[1]);
                decimal ask = decimal.Parse(tokens[2]);
                int bidSize = (int)(decimal.Parse(tokens[3]) * 1000000);
                int askSize = (int)(decimal.Parse(tokens[4]) * 1000000);

                Market m = new Market(timestamp,
                                      ContractId,
                                      bidSize,
                                      bid,
                                      ask,
                                      askSize);

                // Filter out ticks that are more than 15% away from the market.
                if (i == 0 || Math.Abs(m.Mid / lastMid - 1M) < 0.15M)
                {
                    lastMid = m.Mid;
                }
                else
                {
                    lastMid = m.Mid;
                    m = markets[i - 1];
                }

                markets[i] = m;
            }

            return markets;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
