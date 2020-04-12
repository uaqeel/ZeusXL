using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reactive;
using System.Diagnostics;

using CommonTypes;


namespace DataSources
{
    public static class DataSource
    {
        public static IEnumerable<ITimestampedDatum> Create(int ContractId, Dictionary<string, object> Config)
        {
            string dsType = Config["Type"].ToString();
            Type type = Utils.FindType(dsType, "DataSources");
            IEnumerable<ITimestampedDatum> ds = Activator.CreateInstance(type, new object[] { ContractId, Config }) as IEnumerable<ITimestampedDatum>;

            return ds;
        }
    }


    public interface IDataReader : IEnumerable<ITimestampedDatum>
    {
        void Initialise(int contractId, string filename);
    }


    public static class DataSourceUtils
    {
        public static IEnumerable<ITimestampedDatum> Create(string dsType, int contractId, string directory)
        {
            try
            {
                Type type = Utils.FindType(dsType, "DataSources");
                return Activator.CreateInstance(type, new object[] { contractId, directory }) as IEnumerable<ITimestampedDatum>;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }


        public static SortedSet<string> ConstructFileList(string BaseDirectory)
        {
            SortedSet<string> FileList = new SortedSet<string>(); ;
            ExploreDirectory(ref FileList, BaseDirectory);

            return FileList;
        }


        public static void ExploreDirectory(ref SortedSet<string> FileList, string CurrentDirectory)
        {
            FileList.UnionWith(Directory.GetFiles(CurrentDirectory));

            foreach (string dd in Directory.GetDirectories(CurrentDirectory))
            {
                ExploreDirectory(ref FileList, dd);
            }
        }
    }
}
