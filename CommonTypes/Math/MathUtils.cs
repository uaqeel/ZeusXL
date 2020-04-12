using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTypes;


namespace CommonTypes.Maths
{
    public static class MathUtils
    {
        public static double RMS(this IEnumerable<double> Data)
        {
            int nData = Data.Count();

            double t = 0;
            foreach (double d in Data)
            {
                t += d * d;
            }

            double r = t / nData;
            return Math.Sqrt(r);
        }


        public static double Entropy(this IEnumerable<double> Data)
        {
            int nData = Data.Count();

            Dictionary<double, double> nOccurrences = new Dictionary<double, double>();
            foreach (double d in Data)
            {
                nOccurrences[d] += 1 / nData;
            }

            double e = 0;
            foreach (KeyValuePair<double, double> d in nOccurrences)
            {
                e += d.Value * System.Math.Log(d.Value);
            }

            return -e;
        }


        public static int MaxIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T);
            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = value;
                }

                index++;
            }

            return maxIndex;
        }


        public static double[] NextDouble(this Random rng, int dimensionality)
        {
            double[] r = new double[dimensionality];
            for (int i = 0; i < dimensionality; ++i)
            {
                r[i] = rng.NextDouble();
            }

            return r;
        }


        public static double[] Normalise(this double[] data)
        {
            double[] stats = data.Statistics();
            int nData = (int)stats[4];

            if (stats[3] == 0)
                return data;

            double rangeScale = 1 / (stats[1] - stats[0]);

            double[] ret = new double[nData];
            for (int i = 0; i < nData; ++i)
            {
                ret[i] = (data[i] - stats[0]) * rangeScale;
            }

            return ret;
        }
    }
}
