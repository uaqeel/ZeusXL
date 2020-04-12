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
    public class AlexDataSource : GenericFileBasedDataSource<AlexDataReader>
    {
        public AlexDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public AlexDataSource(int contractId, Dictionary<string, object> config)
            : base(contractId, config)
        {
        }
    }

    
    public class AlexDataReader : IDataReader
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
            int dateIndex = -1, bidIndex = -1, askIndex = -1;
            DateTimeOffset baseDate = GetFileBaseDate(filename);
            string[] lines = File.ReadAllLines(filename);

            string[] tokens = lines[0].Split(';');
            for (int i = 0; i < tokens.Length; ++i)
            {
                string t = tokens[i].ToUpper();
                if (t == "ZEIT")
                    dateIndex = i;
                else if (t == "BID")
                    bidIndex = i;
                else if (t == "ASK")
                    askIndex = i;
            }

            Market[] markets = new Market[lines.Length - 1];
            for (int i = 0; i < markets.Length; ++i)
            {
                tokens = lines[i + 1].Split(';');

                // Really stupid.
                int iii = tokens[dateIndex].LastIndexOf(':');
                StringBuilder sb = new StringBuilder(tokens[dateIndex]);
                sb[iii] = '.';
                TimeSpan offset = TimeSpan.Parse(sb.ToString());

                markets[i] = new Market(baseDate + offset,
                                        ContractId,
                                        0,
                                        decimal.Parse(tokens[bidIndex]),
                                        decimal.Parse(tokens[askIndex]),
                                        0);
            }

            return markets;
        }


        private DateTimeOffset GetFileBaseDate(string filename)
        {
            string dateToken = filename.Split('_').Last();
            dateToken = dateToken.Substring(0, dateToken.LastIndexOf('.'));

            DateTimeOffset baseDate;
            DateTimeOffset.TryParse(dateToken, out baseDate);

            if (baseDate == DateTimeOffset.MinValue)
            {
                string[] tokens;
                if (dateToken.Contains('.'))
                {
                    tokens = dateToken.Split('.');
                }
                else
                {
                    tokens = new string[3];
                    tokens[0] = dateToken.Substring(0, 4);
                    tokens[1] = dateToken.Substring(4, 2);
                    tokens[2] = dateToken.Substring(6, 2);
                }

                int year = int.Parse(tokens[0]);
                int month = int.Parse(tokens[1]);
                int day = int.Parse(tokens[2]);

                baseDate = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
            }

            return baseDate;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
