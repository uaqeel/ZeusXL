using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

using CommonTypes;


namespace DataSources
{
    public class DepositRateDataSource: GenericFileBasedDataSource<DepositRateDataReader>
    {
        public DepositRateDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public DepositRateDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    public class DepositRateDataReader : IDataReader
    {
        int NumCurrencies;

        Dictionary<int, string> CurrencyIndices;
        DepositRate[] Rates;


        // Note - ccy is ignored!
        public void Initialise(int contractId, string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("Error, deposit rate data file not found! (" + filename + ")");

            string[] lines = File.ReadAllLines(filename);

            string headers = lines[0];
            string[] headerTokens = headers.Split(',');

            NumCurrencies = headerTokens.Length - 1;
            CurrencyIndices = new Dictionary<int, string>();
            for (int i = 0; i < NumCurrencies; ++i)
            {
                CurrencyIndices[i] = headerTokens[i + 1];
            }

            Rates = new DepositRate[NumCurrencies * (lines.Length - 1)];
            for (int i = 1; i < lines.Length; ++i)
            {
                string[] tokens = lines[i].Split(',');

                DateTimeOffset t = DateTimeOffset.Parse(tokens[0]);

                for (int j = 0; j < NumCurrencies; ++j)
                {
                    double v = double.Parse(tokens[j + 1]);

                    Rates[i * NumCurrencies + j] = new DepositRate(t, CurrencyIndices[j], v);
                }
            }
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            foreach (DepositRate dr in Rates)
            {
                yield return dr;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
