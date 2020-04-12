using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace CommonTypes
{
    [DataContract]
    public class CircularBuffer<T> : IEnumerable<T>
    {
        [DataMember]
        public T[] Data;

        [DataMember]
        public int Size;

        [DataMember]
        public bool Full;

        [DataMember]
        internal int Index;


        public CircularBuffer(int size)
        {
            Size = size;
            Data = new T[size];
            Full = false;
            Index = 0;
        }


        public CircularBuffer(CircularBuffer<T> rhs)
        {
            Size = rhs.Size;
            Data = new T[Size];

            for (int i = 0; i < rhs.Size; ++i)
            {
                Data[i] = rhs.Data[i];
            }

            Full = rhs.Full;
            Index = rhs.Index;
        }


        public void Insert(T v)
        {
            if (Index == Size - 1)
                Full = true;

            Data[Index++ % Size] = v;
        }


        public T this[int n]
        {
            get
            {
                return Data[(n + (Full ? Index : 0)) % Size];
            }
        }


        public T Last()
        {
            return this[Length - 1];
        }


        // Index should be negative.
        public T FromLast(int n)
        {
            return this[Length + n - 1];
        }


        public bool RemoveLast()
        {
            Index--;
            return Index >= 0;
        }


        public int Length
        {
            get
            {
                if (!Full && Index < Size)
                    return Index;
                else
                    return Size;
            }
        }


        public T[] ToArray()
        {
            T[] r = new T[Length];
            for (int i = 0; i < Length; ++i)
            {
                r[i] = this[i];
            }

            return r;
        }


        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; ++i)
            {
                yield return this[i];
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public static class CircularBufferExtensions
    {
        public static double Average(this CircularBuffer<double> cb)
        {
            double sum = 0;
            foreach (double i in cb.Data)
                sum += i;

            return sum / Math.Min(cb.Size, cb.Index);
        }


        // Average absolute deviation within the buffer, NOT from the buffer's mean.
        // Note that this is the L1 norm or taxi-cab distance, which is naturally more
        // conservative than Euclidean distance (L2 norm).
        public static double AD(this CircularBuffer<double> cb)
        {
            double sum = 0, ad = 0;
            if (cb.Size == 1)
                ad = 0;
            else
            {
                int l = Math.Min(cb.Size, cb.Index);
                for (int i = 1; i < l; ++i)
                    sum += Math.Abs(cb.Data[i] - cb.Data[i - 1]);

                ad = sum / l;
            }

            return ad;
        }


        // Sample statistic.
        public static double SD(this CircularBuffer<double> cb)
        {
            double sum = 0;
            int l = Math.Min(cb.Size, cb.Index);
            double a = cb.Average();
            for (int i = 0; i < l; ++i)
                sum += Math.Pow(cb.Data[i] - a, 2);

            return Math.Sqrt(sum / (l - 1));
        }


        // Putting a hurdle in this didn't work: if you're using this as a proxy for Sharpe and the hurdle is
        // bigger than the mean, you get incorrect negative signals.
        public static double Z(this CircularBuffer<double> cb, out double mean)
        {
            double[] stats = cb.Statistics();

            mean = stats[3];
            return (cb.Last() - stats[3]) / stats[4];
        }


        public static double Z(this CircularBuffer<double> cb)
        {
            double mean;
            return cb.Z(out mean);
        }
    }


    public class BiDictionary<TFirst, TSecond>
    {
        IDictionary<TFirst, IList<TSecond>> firstToSecond = new Dictionary<TFirst, IList<TSecond>>();
        IDictionary<TSecond, IList<TFirst>> secondToFirst = new Dictionary<TSecond, IList<TFirst>>();

        private static IList<TFirst> EmptyFirstList = new TFirst[0];
        private static IList<TSecond> EmptySecondList = new TSecond[0];


        public void Add(TFirst first, TSecond second)
        {
            IList<TFirst> firsts;
            IList<TSecond> seconds;
            if (!firstToSecond.TryGetValue(first, out seconds))
            {
                seconds = new List<TSecond>();
                firstToSecond[first] = seconds;
            }
            if (!secondToFirst.TryGetValue(second, out firsts))
            {
                firsts = new List<TFirst>();
                secondToFirst[second] = firsts;
            }
            seconds.Add(second);
            firsts.Add(first);
        }


        public ICollection<TFirst> Keys
        {
            get { return firstToSecond.Keys; }
        }


        // Note potential ambiguity using indexers (e.g. mapping from int to int)
        // Hence the methods as well...
        public IList<TSecond> this[TFirst first]
        {
            get { return GetByFirst(first); }
        }


        public IList<TFirst> this[TSecond second]
        {
            get { return GetBySecond(second); }
        }


        public IList<TSecond> GetByFirst(TFirst first)
        {
            IList<TSecond> list;
            if (!firstToSecond.TryGetValue(first, out list))
            {
                return EmptySecondList;
            }
            return new List<TSecond>(list); // Create a copy for sanity
        }


        public IList<TFirst> GetBySecond(TSecond second)
        {
            IList<TFirst> list;
            if (!secondToFirst.TryGetValue(second, out list))
            {
                return EmptyFirstList;
            }
            return new List<TFirst>(list); // Create a copy for sanity
        }


        public bool ContainsFirst(TFirst first)
        {
            return firstToSecond.ContainsKey(first);
        }


        public bool ContainsSecond(TSecond second)
        {
            return secondToFirst.ContainsKey(second);
        }
    }


    public class SerialisableDictionary : Dictionary<string, object>
    {
        public SerialisableDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        { }


        public SerialisableDictionary(SerialisableDictionary rhs)
            : this()
        {
            foreach (var kv in rhs)
            {
                this[kv.Key] = kv.Value;
            }
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var k in Keys)
            {
                sb.Append(k + "," + this[k].GetType() + "," + this[k].ToString() + Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
