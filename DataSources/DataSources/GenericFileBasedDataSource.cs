using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

using CommonTypes;


namespace DataSources
{
    // This is a generics-based data source for serving ITimeStampedDatum objects. Implement IDataReader for your
    // particular data format and then use this to serve the data.
    public class GenericFileBasedDataSource<T> : IEnumerable<ITimestampedDatum> where T : IDataReader, new()
    {
        public int ContractId { get; set; }

        string BaseDirectory;
        SortedSet<string> FileList;
        int NumFilesToLoad;

        string Currency;

        Queue<T> Q;
        Thread Producer;

        bool doneReading;


        public GenericFileBasedDataSource(int contractId, string baseDirectory, int numFilesToLoad)
        {
            ContractId = contractId;
            BaseDirectory = baseDirectory;

            FileList = DataSourceUtils.ConstructFileList(BaseDirectory);
            NumFilesToLoad = numFilesToLoad;

            doneReading = false;
        }


        public GenericFileBasedDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString(), config.GetOrDefault("NumFilesToLoad", 10).AsInt())
        {
            Currency = config.GetOrDefault("Currency", "Base");
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            if (Producer == null)
                Read();

            while (Producer.IsAlive || Q.Count > 0 || !doneReading)
            {
                if (Q.Count > 0)
                {
                    T ddr = Q.Dequeue();
                    foreach (ITimestampedDatum m in ddr)
                    {
                        yield return m;
                    }
                }
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private void Read()
        {
            Q = new Queue<T>(NumFilesToLoad * 10);
            Producer = new Thread(() =>
            {
                foreach (string f in FileList)
                {
                    while (Q.Count > NumFilesToLoad)
                        Thread.Sleep(1000);

                    T reader = new T();
                    reader.Initialise(ContractId, f);
                    Q.Enqueue(reader);
                }

                doneReading = true;
            });
            Producer.IsBackground = true;
            Producer.Priority = ThreadPriority.BelowNormal;
            Producer.Start();
        }
    }
}
