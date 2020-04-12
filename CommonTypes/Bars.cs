using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Diagnostics;

using ProtoBuf;
using CommonTypes;


namespace CommonTypes
{
    [ProtoContract]
    public class Bar : EventArgs, ITimestampedDatum
    {
        public DateTimeOffset StartTime { get; protected set; }
        [ProtoMember(1)]
        public string StartTimeString
        {
            get { return StartTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"); }
            set { StartTime = DateTimeOffset.Parse(value); }
        }

        [ProtoMember(2)]
        public Market LastMarket { get; protected set; }

        [ProtoMember(3)]
        public int NumTicks { get; protected set; }

        [ProtoMember(4)]
        public decimal Open { get; protected set; }

        [ProtoMember(5)]
        public decimal Low { get; protected set; }

        [ProtoMember(6)]
        public decimal High { get; protected set; }


        // StartDate and OpeningMarket.Timestamp don't have to be the same thing. This is so we can produce
        // proper coverings even when there are periods with no ticks. In these instances, the last market
        // provides the values for the bar, and the StartDate represents the starting time of the interval
        // with no ticks.
        public Bar(DateTimeOffset StartDate, Market OpeningMarket)
        {
            StartTime = StartDate;
            NumTicks = 0;

            decimal mid = OpeningMarket.Mid;
            Open = Low = High = mid;

            LastMarket = OpeningMarket;
        }


        // For ProtoBuf.
        protected Bar()
        {
        }


        // Use this when you need to copy a bar but overwrite the end time (for example, to make the bar end at exactly the
        // end of an hour or something).
        public Bar(Bar rhs, DateTimeOffset endTime)
        {
            StartTime = rhs.StartTime;
            NumTicks = rhs.NumTicks;

            Open = rhs.Open;
            Low = rhs.Low;
            High = rhs.High;

            LastMarket = new Market(rhs.LastMarket);
            LastMarket.Timestamp = endTime;
        }


        public Bar(Bar rhs, DateTimeOffset startTime, DateTimeOffset endTime, int numTicks)
            : this(rhs, endTime)
        {
            StartTime = startTime;
            NumTicks = numTicks;
        }


        public Bar(DateTimeOffset startTime, DateTimeOffset endTime, int contractId, decimal open, decimal low, decimal high, decimal close)
        {
            StartTime = startTime;
            NumTicks = 0;

            Open = open;
            Low = low;
            High = high;

            LastMarket = new Market(endTime, contractId, close, close);
        }


        public void Update(Market x)
        {
            decimal mid = x.Mid;

            if (mid < Low)
                Low = mid;
            else if (mid > High)
                High = mid;

            LastMarket = x;
            NumTicks++;
        }


        public void Update(Price x)
        {
            Update(x.ToMarket());
        }


        public int ContractId
        {
            get
            {
                if (LastMarket == null)
                    throw new Exception("Error, Bar with no ContractId!");
                else
                    return LastMarket.ContractId;
            }
        }


        public decimal Close
        {
            get
            {
                if (LastMarket != null)
                    return LastMarket.Mid;
                else
                    return decimal.MinValue;
            }
        }


        public DateTimeOffset EndTime
        {
            get
            {
                if (LastMarket != null)
                    return LastMarket.Timestamp;
                else
                    return DateTimeOffset.MaxValue;
            }
        }


        public DateTimeOffset Timestamp
        {
            get
            {
                return EndTime;
            }
        }


        public TimeSpan Length
        {
            get
            {
                return (EndTime - StartTime);
            }
        }


        public decimal Efficiency
        {
            get
            {
                // |Close - Open| / (|Open - High| + |High - Low| + |Low - Close|)
                if (Close != Open)
                    return Math.Abs(Close - Open) / (Math.Abs(Open - High) + Math.Abs(High - Low) + Math.Abs(Low - Close));
                else
                    return 0M;
            }
        }


        public override string ToString()
        {
            int contractId = (LastMarket == null ? -1 : LastMarket.ContractId);
            return string.Format("{2} - {0} = {1:0.00000} (O:{3:0.00000},L:{4:0.00000},H:{5:0.00000},C:{6:0.00000}) on {7} ticks / {8:0.00}s",
                                 contractId, Close /*Average*/, EndTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), Open, Low, High, Close, NumTicks, Length.TotalSeconds);
        }


        public string ToCSV()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6}", Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff"), Open, Low, High, Close, LastMarket.Bid, LastMarket.Ask);
        }


        // Note: is not responsible for gaps or overlaps between the two bars being merged, so it's the user's job
        // to ensure the Bars cover disjoint but consecutive periods.
        public Bar Merge(Bar rhs)
        {
            Bar newBar = new Bar(this, this.EndTime);

            bool thisIsFirst = (EndTime < rhs.EndTime);

            if (thisIsFirst)
            {
                newBar.LastMarket = rhs.LastMarket;
            }
            else
            {
                newBar.StartTime = rhs.StartTime;
                newBar.Open = rhs.Open;
            }

            newBar.Low = Math.Min(Low, rhs.Low);
            newBar.High = Math.Max(High, rhs.High);

            //newBar.Average = (NumTicks * Average + rhs.NumTicks * rhs.Average) / (NumTicks + rhs.NumTicks);
            //newBar.AverageSpread = (NumTicks * AverageSpread + rhs.NumTicks * rhs.AverageSpread) / (NumTicks + rhs.NumTicks);
            //newBar.MaxSpread = Math.Max(MaxSpread, rhs.MaxSpread);

            newBar.NumTicks += rhs.NumTicks;

            return newBar;
        }
    }


    public interface IBarList : IEnumerable<Bar>
    {
        // This event is fired whenever a new bar is inserted.
        EventHandler<Bar> BarFinalisedListeners { get; set; }
    }


    public class BarList : IBarList
    {
        IMessageBus Ether;

        public int ContractId;

        public int MaxBars;

        public int BarLength;

        public CircularBuffer<Bar> Bars;

        public EventHandler<Bar> BarFinalisedListeners { get; set; }

        
        public BarList(IMessageBus ether, int contractId, int numBarsToStore, int barLengthInSeconds)
        {
            Ether = ether;
            if (barLengthInSeconds < Ether.EpochSecs)
                throw new Exception("Error, bars shorter than " + Ether.EpochSecs + "s are not possible!");

            if (barLengthInSeconds % Ether.EpochSecs != 0)
                throw new Exception("Error, bar length must be a multiple of " + Ether.EpochSecs + "s");

            ContractId = contractId;
            MaxBars = numBarsToStore;
            BarLength = barLengthInSeconds;

            Bars = new CircularBuffer<Bar>(MaxBars);

            // Subscribe to the contract's ticks.
            Ether.AsObservable<Market>().Where(x => x.ContractId == ContractId).Subscribe(x =>
            {
                Update(x);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
                throw e;
            });

            // Subscribe to the epoch heartbeats that will signal the completion of bars.
            Ether.AsObservable<Heartbeat>().Where(x => x.Timestamp.TimeOfDay.TotalSeconds % BarLength == 0).Subscribe(x =>
            {
                CompleteBar(x.Timestamp);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
                throw e;
            });
        }


        private void CompleteBar(DateTimeOffset timestamp)
        {
            if (Bars.Length == 0)
                return;

            if (BarFinalisedListeners != null && Bars.Last().NumTicks > 0)
            {
                Bar bb = new Bar(Bars.Last(), timestamp);
                BarFinalisedListeners(this, bb);
            }

            // Remove the bar from the list if it's empty. Note that this means
            // users need to check BarStartTimes, because there's no guarantee
            // bars provide a covering of the period.
            if (Bars.Last().NumTicks == 0)
            {
                Bars.RemoveLast();
            }

            Bars.Insert(new Bar(timestamp, Bars.Last().LastMarket));
        }


        private void Update(Market mkt)
        {
            if (Bars.Length == 0)
            {
                Bars.Insert(new Bar(mkt.Timestamp, mkt));
            }

            Bars.Last().Update(mkt);
        }


        public int Length
        {
            get { return Bars.Length; }
        }


        public Bar this[int i]
        {
            get { return Bars[i]; }
        }


        public virtual IEnumerator<Bar> GetEnumerator()
        {
            return Bars.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
