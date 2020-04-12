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
    public class TimeAndSalesDataSource : GenericFileBasedDataSource<TimeAndSalesDataReader>
    {
        public TimeAndSalesDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 10)
        {
        }


        public TimeAndSalesDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    public class TimeAndSalesDataReader : IDataReader
    {
        int ContractId;
        TimeAndSales[] TimeAndSales;


        // Note - ccy is ignored!
        public void Initialise(int contractId, string filename)
        {
            ContractId = contractId;
            TimeAndSales = ReadFile(filename);
        }


        private TimeAndSales[] ReadFile(string filename)
        {
            int dateIndex = 2, timeIndex = 3, priceIndex = 4, volumeIndex = 5;
            string[] lines = File.ReadAllLines(filename);

            TimeAndSales[] tas = new TimeAndSales[lines.Length];
            for (int i = 0; i < tas.Length; ++i)
            {
                string[] tokens = lines[i].Split(',');

                DateTime date = DateTime.Parse(tokens[dateIndex]);
                TimeSpan time = TimeSpan.Parse(tokens[timeIndex]);
                decimal price = decimal.Parse(tokens[priceIndex]);
                int volume = int.Parse(tokens[volumeIndex]);

                tas[i] = new TimeAndSales(date.Add(time), ContractId, price, volume);
            }

            return tas;
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            foreach (TimeAndSales tas in TimeAndSales)
            {
                yield return tas;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
