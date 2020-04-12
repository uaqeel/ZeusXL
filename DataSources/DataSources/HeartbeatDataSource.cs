using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using CommonTypes;


// Note that gaps in the HeartbeatDataSource are not acceptable -- it'll screw up Sharpe (etc.) annualisations and possibly other things too.
namespace DataSources
{
    public class HeartbeatDataSource : IEnumerable<ITimestampedDatum>
    {
        public DateTimeOffset StartDate, EndDate;
        public int Spacing;
        public string Message;

        DateTimeOffset NextHeartbeatTime;


        public HeartbeatDataSource(string baseDirectory)
        {
            string firstFile = Directory.EnumerateFiles(baseDirectory).First();
            string[] lines = File.ReadAllLines(firstFile);

            string[] tokens = lines[0].Split(',');

            StartDate = tokens[0].AsDate();
            EndDate = tokens[1].AsDate();
            Spacing = int.Parse(tokens[2]);
            Message = tokens[3];

            NextHeartbeatTime = StartDate.Date.AddSeconds(Spacing * Math.Ceiling(StartDate.TimeOfDay.TotalSeconds / Spacing));
        }


        public HeartbeatDataSource(DateTimeOffset startDate, DateTimeOffset endDate, int spacing, string message)
        {
            StartDate = startDate;
            EndDate = endDate;
            Spacing = spacing;
            Message = message;

            NextHeartbeatTime = StartDate.Date.AddSeconds(Spacing * Math.Ceiling(StartDate.TimeOfDay.TotalSeconds / Spacing));
        }


        public HeartbeatDataSource(DateTimeOffset startDate, DateTimeOffset endDate, int spacing)
            : this(startDate, endDate, spacing, "Epoch Heartbeat") { }


        // Same signature as other IEnumerable<ITimestampedDatum> constructors so that this can be instantiated easily...
        public HeartbeatDataSource(int contractId, string baseDirectory)
            : this(baseDirectory)
        {
        }


        public IEnumerator<ITimestampedDatum> GetEnumerator()
        {
            do
            {
                yield return new Heartbeat(NextHeartbeatTime, Spacing, Message);

                NextHeartbeatTime = NextHeartbeatTime.Add(TimeSpan.FromSeconds(Spacing));
            } while (NextHeartbeatTime < EndDate);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
