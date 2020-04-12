using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reactive;

using CommonTypes;


namespace DataSources
{
    public class DataCollator
    {
        int EpochSecs;
        int nDataSources;
        List<IEnumerable<ITimestampedDatum>> DataSources = new List<IEnumerable<ITimestampedDatum>>();
        List<IEnumerator<ITimestampedDatum>> Enumerators = new List<IEnumerator<ITimestampedDatum>>();

        SortedDictionary<DateTimeOffset, List<int>> DataQueue = new SortedDictionary<DateTimeOffset, List<int>>();

        // This keeps track of the number of enumerators that are done serving data -- once this number reaches
        // nDataSources - 1, we know that we can terminate the DataCollator.
        int FinishedEnumerators = 0;


        public DataCollator(IEnumerable<ITimestampedDatum>[] dataSources, int epochSecs)
        {
            EpochSecs = epochSecs;

            bool IsEpochHeartbeat = false;
            foreach (var dd in dataSources)
            {
                if (dd.GetType() == typeof(HeartbeatDataSource))
                {
                    HeartbeatDataSource t = (HeartbeatDataSource)dd;
                    if (t.Spacing == epochSecs)
                        IsEpochHeartbeat = true;
                }

                if (!IsEpochHeartbeat)
                {
                    // Add the data source to our list.
                    DataSources.Add(dd);

                    // Add to list of enumerators.
                    AddDataSourceEnumeratorToList(nDataSources);

                    nDataSources++;
                }

                IsEpochHeartbeat = false;
            }

            // Initialise an epoch heartbeat. This is always necessary since we've excluded any other epoch-spaced
            // HeartBeats (if present). Note that I'm setting epochStartDate to the start of the day. This is so that
            // results for multi-strategy+multi-contract setups match those for a single-strategy setup (eg, if the
            // AUDUSD dataset starts 5 seconds before EURUSD, running a strategy on EURUSD in the two setups will 
            // give slightly different Sharpe etc. because the length of the dataset is slightly different).
            DateTimeOffset epochStartDate = (DataQueue.Count > 0 ? DataQueue.Keys.First() : DateTimeOffset.UtcNow).Date;
            DataSources.Add(new HeartbeatDataSource(epochStartDate, DateTime.MaxValue, EpochSecs));
            AddDataSourceEnumeratorToList(nDataSources);

            nDataSources++;

            // If we only have an epoch heartbeat in our list of data sources, then we assume this
            // is a live run and never terminate.
            // TODO(cleanup-3) -- more think about formalising than clean up...
            if (nDataSources == 1)
                FinishedEnumerators = -1;
        }


        public DataCollator(IEnumerable<ITimestampedDatum> dataSource, int epochSecs)
            : this(new IEnumerable<ITimestampedDatum>[] { dataSource }, epochSecs)
        {
        }


        private void AddDataSourceEnumeratorToList(int index)
        {
            Enumerators.Add(DataSources[index].GetEnumerator());

            if (Enumerators[index].MoveNext())
            {
                ITimestampedDatum current = Enumerators[index].Current;
                if (!DataQueue.ContainsKey(current.Timestamp))
                {
                    DataQueue.Add(current.Timestamp, new List<int>());
                }

                DataQueue[current.Timestamp].Add(index);
            }
        }


        public ITimestampedDatum GetNextDatum()
        {
            if (DataQueue.Count > 0)
            {
                KeyValuePair<DateTimeOffset, List<int>> firstTime = DataQueue.First();

                int sourceId = firstTime.Value.First();

                // Get the datum and advance the source's enumerator to see what the next timestamp it has is.
                ITimestampedDatum datum = Enumerators[sourceId].Current;

                // Remove the datum we've just retrieved from the queue.
                if (DataQueue[firstTime.Key].Count == 1)
                {
                    DataQueue.Remove(firstTime.Key);
                }
                else
                {
                    DataQueue[firstTime.Key].RemoveAt(0);                   // Since sourceId is retrieved by First(), this makes sense.
                }

                // We want to stop adding to the data queue if the only data being served are the epoch heartbeats.
                if (FinishedEnumerators != nDataSources - 1)
                {
                    // Add the source's next timestamp to the data queue.
                    if (Enumerators[sourceId].MoveNext())
                    {
                        DateTimeOffset nextDate = Enumerators[sourceId].Current.Timestamp;

                        if (!DataQueue.ContainsKey(nextDate))
                        {
                            DataQueue.Add(nextDate, new List<int>());
                        }

                        DataQueue[nextDate].Add(sourceId);
                    }
                    else
                    {
                        FinishedEnumerators++;
                    }
                }

                return datum;
            }

            return null;
        }


        public static List<Type> GetDataSourceTypes()
        {
            List<Type> ret = new List<Type>();

            foreach (Type t in Assembly.GetAssembly(typeof(DataCollator)).GetTypes())
            {
                if (t.GetInterfaces().Contains(typeof(IEnumerable<ITimestampedDatum>)))
                {
                    ret.Add(t);
                }
            }

            return ret;
        }
    }
}
