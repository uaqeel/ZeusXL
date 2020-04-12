using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace CommonTypes
{
    // Online descriptive statistics for very long samples. Not much use for moving windows though - use CircularBuffers there.
    public class SummaryContainer
    {
        public int Count;
        public double Min;
        public double Max;
        public double Average;
        public double Stdev
        {
            get
            {
                if (varVariable == 0) return 0;
                return Math.Sqrt(varVariable / (Count - 1));
            }
        }
        public double Total;
        public double First;
        public double Last;

        private double varVariable;


        public SummaryContainer()
        {
            Count = 0;
            Last = 0;                                           // This should be NaN, but then I'd have to check Count before using it...
        }

        public void Add(double datum)
        {
            if (Count == 0)
            {
                Min = Max = Average = Total = Last = datum;
                varVariable = 0;
                First = datum;
            }
            else
            {
                if (datum < Min) Min = datum;
                else if (datum > Max) Max = datum;

                double d = datum - Average;
                Total += datum;

                Average += d / (Count + 1);
                varVariable += d * (datum - Average);

                Last = datum;
            }

            Count++;
        }


        public double Z
        {
            get
            {
                if (Stdev == 0)
                    return 0;
                else
                    return Average / Stdev;
            }
        }


        public double StandardError
        {
            get
            {
                if (Stdev == 0)
                    return 0;
                else
                    return Stdev / Math.Sqrt(Count);
            }
        }
    }


    public class DependenceContainer
    {
        public int Count { get; private set; }
        double RunningProduct;
        SummaryContainer X;
        SummaryContainer Y;
        public double Covariance { get; set; }


        public DependenceContainer()
        {
            Count = 0;
            RunningProduct = 0;
            X = new SummaryContainer();
            Y = new SummaryContainer();
        }


        public void Add(double x, double y)
        {
            Count++;
            RunningProduct += x * y;

            X.Add(x);
            Y.Add(y);

            Covariance = RunningProduct / Count - X.Average * Y.Average;
        }


        public double Correlation
        {
            get
            {
                double population_adjustment_factor = (Count - 1.0) / Count;

                return Covariance / (X.Stdev * Y.Stdev * population_adjustment_factor);
            }
        }


        public double R2
        {
            get
            {
                return Math.Pow(Correlation, 2);
            }
        }


        public double FirstX
        {
            get
            {
                return X.First;
            }
        }


        public double LastX
        {
            get
            {
                return X.Last;
            }
        }
    }


    public class DistributionContainer
    {
        public double Epsilon;                                          // Bucket width.
        public SortedDictionary<double, int> Buckets;
        int Total;

        int round;


        public DistributionContainer(double epsilon)
        {
            Epsilon = epsilon;
            Buckets = new SortedDictionary<double, int>();
            Total = 0;

            round = (int)Math.Ceiling(-Math.Log10(Epsilon));
        }


        public DistributionContainer(Dictionary<string, object> config)
            : this(config.GetOrDefault("Epsilon", 0.001).AsDouble())
        { }


        public void Add(double newValue)
        {
            double nv = Math.Round(newValue, round);
            if (!Buckets.ContainsKey(nv))
                Buckets[nv] = 0;

            Buckets[nv]++;
            Total++;
        }


        // By default, removes probability mass from bottom of distribution.
        public void Remove(double percentage, bool fromTop)
        {
            int numToRemove = (int)(percentage * Total);
            int left = Total - numToRemove;

            double[] keys = null;
            if (!fromTop)
                keys = Buckets.Keys.ToArray();
            else
                keys = Buckets.Keys.Reverse().ToArray();

            foreach (double key in keys)
            {
                if (numToRemove >= Buckets[key])
                {
                    numToRemove -= Buckets[key];
                    Buckets.Remove(key);
                }
                else
                {
                    Buckets[key] -= numToRemove;
                    numToRemove = 0;

                    break;
                }
            }

            Total = left;
        }


        public double GetPercentile(double value)
        {
            return Buckets.Sum(x => x.Key < value ? x.Value : 0) / Total;
        }


        public double GetValue(double percentile)
        {
            int n = (int)(Total * percentile);

            int t = 0;
            foreach (var kv in Buckets)
            {
                t += kv.Value;

                if (t >= n)
                    return kv.Key;
            }

            return -double.MaxValue;
        }


        public int Count
        {
            get { return (int)Total; }
        }
    }
}
