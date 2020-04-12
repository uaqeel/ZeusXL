using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Statistics;


namespace CommonTypes
{
    public static class ContainerUtils
    {
        public static List<List<T>> Split<T>(this IEnumerable<T> Source, int ChunkLength)
        {
            return Source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / ChunkLength)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }


        // If T is numerical, make it a double. Please.
        public static T GetOrDefault<K, V, T>(this IDictionary<K, V> Source, K Key, T DefaultValue)
        {
            if (Source.ContainsKey(Key))
                return (T)Convert.ChangeType(Source[Key], typeof(T));
            else
                return DefaultValue;
        }


        public static T Get<K, T>(this IDictionary<K, object> Source, K Key)
        {
            if (!Source.ContainsKey(Key))
                throw new Exception(string.Format("Error, key '{0}' not found in config dictionary!", Key.ToString()));

            return GetOrDefault(Source, Key, default(T));
        }


        public static T[] Row<T>(this T[,] matrix, int rowIndex)
        {
            int nColumns = matrix.GetLength(1);

            T[] ret = new T[nColumns];
            for (int i = 0; i < nColumns; ++i)
                ret[i] = matrix[rowIndex, i];

            return ret;
        }

        public static T[] Column<T>(this T[,] matrix, int columnIndex)
        {
            int nRows = matrix.GetLength(0);

            T[] ret = new T[nRows];
            for (int i = 0; i < nRows; ++i)
                ret[i] = matrix[i, columnIndex];

            return ret;
        }


        // Like a List<double[]>.
        public static T[] Column<T>(this IEnumerable<T[]> matrix, int columnIndex)
        {
            List<T> column = new List<T>();

            foreach (T[] x in matrix)
                column.Add(x[columnIndex]);

            return column.ToArray();
        }


        public static double[] Statistics(this IEnumerable<double> vector)
        {
            double[] ret = new double[6];                        // Count, Min, Max, Average, Stdev, Total.

            double s = 0, s_2 = 0;
            ret[0] = 0;
            ret[1] = double.MaxValue;
            ret[2] = double.MinValue;

            foreach (double d in vector)
            {
                s += d;
                s_2 += d * d;

                if (d < ret[1])
                    ret[1] = d;
                else if (d > ret[2])
                    ret[2] = d;

                ret[0]++;
            }

            ret[3] = s / ret[0];
            ret[4] = Math.Sqrt(s_2 / (ret[0] - 1) - ret[3] * ret[3]);
            ret[5] = s;

            return ret;
        }


        public static void FillWithEmptyStrings(this object[,] inMatrix)
        {
            int r = inMatrix.GetLength(0), c = inMatrix.GetLength(1);

            for (int i = 0; i < r; ++i)
            {
                for (int j = 0; j < c; ++j)
                {
                    inMatrix[i, j] = "";
                }
            }
        }


        public static decimal[] FirstDifferences(this decimal[] inVector)
        {
            decimal[] ret = new decimal[inVector.Length - 1];

            for (int i = 0; i < inVector.Length - 1; ++i)
            {
                ret[i] = inVector[i + 1] - inVector[i];
            }

            return ret;
        }


        public static double AsDouble(this object doubleValue)
        {
            return (double)doubleValue;
        }


        public static int AsInt(this object intValue)
        {
            return (int)intValue.AsDouble();
        }


        // This selects successive pairs from an IEnumerable.
        public static IEnumerable<TResult> SelectPairs<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> selector)
        {
            using (IEnumerator<TSource> iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext()) { yield break; }
                TSource prev = iterator.Current;
                while (iterator.MoveNext())
                {
                    TSource current = iterator.Current;
                    yield return selector(prev, current);
                    prev = current;
                }
            }
        }


        public static string ToCSV(this Dictionary<string, object> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kv in data)
            {
                sb.Append(kv.Key + "," + kv.Value.GetType() + "," + kv.Value.ToString() + Environment.NewLine);
            }

            return sb.ToString();
        }


        public static string ToCSV<T>(this T[,] data)
        {
            StringBuilder sb = new StringBuilder();
            int nRows = data.GetLength(0), nCols = data.GetLength(1);
            for (int r = 0; r < nRows; ++r)
            {
                sb.AppendLine(string.Join(",", data.Row(r)));
            }

            return sb.ToString();
        }


        public static IEnumerable<IList<T>> Permute<T>(this IList<T> list, int length)
        {

            if (list == null || list.Count == 0 || length <= 0)
            {
                yield break;
            }

            if (length > list.Count)
            {
                throw new ArgumentOutOfRangeException("length must be between 1 and the length of the list inclusive");
            }

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var initial = new[] { item };
                if (length == 1)
                {
                    yield return initial;
                }
                else
                {
                    foreach (var variation in Permute(list.Where((x, index) => index != i).ToList(), length - 1))
                    {
                        yield return initial.Concat(variation).ToList();
                    }
                }
            }
        }


        #region ParallelWhile implementation
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition)
        {
            while (condition()) yield return true;
        }


        public static void ParallelWhile(ParallelOptions parallelOptions, Func<bool> condition, System.Action body)
        {
            Parallel.ForEach(IterateUntilFalse(condition), parallelOptions,
                ignored => body());
        }
        #endregion
    }
}
