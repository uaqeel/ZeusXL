using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

using ProtoBuf;
using CommonTypes;


namespace DataSources
{
    public class BarDataSource : GenericFileBasedDataSource<BarDataSeries>
    {
        public BarDataSource(int contractId, string baseDirectory)
            : base(contractId, baseDirectory, 3)
        {
        }


        public BarDataSource(int contractId, Dictionary<string, object> config)
            : this(contractId, config["BaseDirectory"].ToString())
        {
        }
    }


    [ProtoContract]
    public class BarDataSeries : IDataReader
    {
        int ContractId;

        [ProtoMember(1)]
        Bar[] Bars;


        // For ProtoBuf.
        public BarDataSeries()
        { }


        public BarDataSeries(int contractId, Bar[] bars)
        {
            ContractId = contractId;

            Bars = bars.OrderBy(x => x.Timestamp).ToArray();
        }


        public void Initialise(int contractId, string filename)
        {
            ContractId = contractId;

            using (var fs = new FileStream(filename, FileMode.Open))
            using (var gs = new GZipStream(fs, CompressionMode.Decompress))
            {
                BarDataSeries rhs = Serializer.Deserialize<BarDataSeries>(gs);

                Bars = rhs.Bars;
            }
        }


        public void Save(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.OpenOrCreate))
            using (var gs = new GZipStream(fs, CompressionMode.Compress))
            {
                Serializer.Serialize(gs, this);
            }
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            foreach (Bar m in Bars)
            {
                m.LastMarket.ContractId = ContractId;
                yield return m;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


// TODO(research) - http://ensign.editme.com/rangebars