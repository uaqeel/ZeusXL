using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Reactive;

using CommonTypes;
using DataSources;


namespace DataSources
{
    public class GAINDataSource : GenericFileBasedDataSource<GAINDataReader>
    {
        public GAINDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public GAINDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    public class GAINDataReader : IDataReader
    {
        public int ContractId { get; set; }
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
            int dateIndex = -1, bidIndex = -1, askIndex = -1, timeIdentifierIndex = -1;
            string[] lines = File.ReadAllLines(filename);

            string[] tokens = lines[0].Split(',');
            for (int i = 0; i < tokens.Length; ++i)
            {
                string t = tokens[i].ToUpper();
                if (t == "RATEDATETIME")
                    dateIndex = i;
                else if (t == "RATEBID")
                    bidIndex = i;
                else if (t == "RATEASK")
                    askIndex = i;
                else if (t == "LTID")
                    timeIdentifierIndex = i;
            }

            Market[] markets = new Market[lines.Length - 1];
            for (int i = 0; i < markets.Length; ++i)
            {
                tokens = lines[i + 1].Split(',');
                markets[i] = new Market(tokens[dateIndex].AsDate().AddTicks(long.Parse(tokens[timeIdentifierIndex]) % 1000),
                                        ContractId,
                                        0,
                                        decimal.Parse(tokens[bidIndex]),
                                        decimal.Parse(tokens[askIndex]),
                                        0);
            }

            return markets;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
