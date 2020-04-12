using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

using CommonTypes;


namespace DataSources
{
    public class EODBarDataSource : GenericFileBasedDataSource<EODBarDataReader>
    {
        public EODBarDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public EODBarDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    public class EODBarDataReader : IDataReader
    {
        int ContractId;
        Bar[] Bars;


        public void Initialise(int contractId, string filename)
        {
            ContractId = contractId;

            if (!File.Exists(filename))
                throw new Exception("Error, EOD bar file doesn't exist! (" + filename + ")");

            string[] lines = File.ReadAllLines(filename);

            int openIndex = -1, lowIndex = -1, highIndex = -1, closeIndex = -1;
            string[] headerTokens = lines[0].Split(',');

            int i = 0;
            foreach (string h in headerTokens)
            {
                if (h.Equals("Open", StringComparison.OrdinalIgnoreCase))
                    openIndex = i;
                else if (h.Equals("Close", StringComparison.OrdinalIgnoreCase))
                    closeIndex = i;
                else if (h.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    lowIndex = i;
                else if (h.Equals("High", StringComparison.OrdinalIgnoreCase))
                    highIndex = i;

                i++;
            }

            if (openIndex == -1 || lowIndex == -1 || highIndex == -1 || closeIndex == -1)
                throw new Exception("Error, missing column in EOD bar file - doesn't contain complete OLHC data!");

            Bars = new Bar[lines.Length - 1];
            for (i = 1; i < lines.Length; ++i)
            {
                string[] tokens = lines[i].Split(',');
                DateTimeOffset t = DateTimeOffset.Parse(tokens[0]);

                decimal open = decimal.Parse(tokens[openIndex]);
                decimal low = decimal.Parse(tokens[lowIndex]);
                decimal high = decimal.Parse(tokens[highIndex]);
                decimal close = decimal.Parse(tokens[closeIndex]);

                Bars[i] = new Bar(t.AddDays(-1), t, ContractId, open, low, high, close);
            }
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            foreach (Bar b in Bars)
            {
                yield return b;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
