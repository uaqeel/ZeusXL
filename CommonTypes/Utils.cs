using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;


namespace CommonTypes
{
    public static class Utils
    {
        public static Type FindType(string typeName, string namespaceOpt)
        {
            List<Type> candidates = new List<Type>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                if (a.FullName.StartsWith("Microsoft") || a.FullName.StartsWith("System") || a.FullName.StartsWith("ExcelDna"))
                    continue;

                if (a.IsDynamic)
                    continue;

                foreach (Type t in a.GetExportedTypes())
                {
                    if (t.Name == typeName || t.FullName == typeName)
                    {
                        candidates.Add(t);
                    }
                }
            }

            if (candidates.Count() == 0)
            {
                Debug.WriteLine("Error - type '" + typeName + "' could not be found in loaded assemblies! Check strategy classes have been marked 'public' and are serializable.");
                throw new Exception("Error - type '" + typeName + "' could not be found in loaded assemblies! Check strategy classes have been marked 'public' and are serializable.");
            }
            else if (candidates.Count() != 1)
            {
                Type namespaceBasedMatch = null;
                foreach (Type tt in candidates) {
                    if (tt.Namespace == namespaceOpt)
                        namespaceBasedMatch = tt;
                }

                if (namespaceBasedMatch == null)
                {
                    Debug.WriteLine("Error - " + candidates.Count() + " possible matches for type '" + typeName + "' found!");
                    throw new Exception("Error - " + candidates.Count() + " possible matches for type '" + typeName + "' found!");
                }
                else
                {
                    candidates.Clear();
                    candidates.Insert(0, namespaceBasedMatch);
                }
            }

            return candidates[0];
        }


        public static DateTimeOffset AsDate(this object input)
        {
            string datestring = input.ToString();
            DateTimeOffset dto;

            if (!DateTimeOffset.TryParse(datestring, out dto))
            {
                dto = (DateTimeOffset)DateTime.FromOADate(double.Parse(datestring));
            }

            return dto;
        }


        // Use ZView to log data to be graphed.
        public static void ZView(DateTimeOffset dto, string SeriesName, double value, bool IsSmallData, bool IsAuxiliaryData, bool IsPoint)
        {
            Trace.WriteLine(string.Format("{0},{1},{2:0.000000},{3},{4},{5}",
                            dto.UtcDateTime.ToString("yyyy-MMM-dd HH:mm:ss.000"), SeriesName, value, IsSmallData, IsAuxiliaryData, IsPoint));
        }


        // Use ZView to log data to be graphed.
        public static void ZView(DateTimeOffset dto, string SeriesName, decimal value, bool IsSmallData, bool IsAuxiliaryData, bool IsPoint)
        {
            ZView(dto, SeriesName, (double)value, IsSmallData, IsAuxiliaryData, IsPoint);
        }


        public static void ZView(DateTimeOffset dto, string SeriesName, double value, bool IsSmallData, bool IsAuxiliaryData)
        {
            ZView(dto, SeriesName, value, IsSmallData, IsAuxiliaryData, false);
        }


        public static void ZView(DateTimeOffset dto, string SeriesName, decimal value, bool IsSmallData, bool IsAuxiliaryData)
        {
            ZView(dto, SeriesName, value, IsSmallData, IsAuxiliaryData, false);
        }


        // Use ZView to log data to be printed but not plotted.
        public static void ZLog(DateTimeOffset dto, string SeriesName, double value)
        {
            Trace.WriteLine(string.Format("{0},{1},{2}", dto.UtcDateTime.ToString("yyyy-MMM-dd HH:mm:ss.000"), SeriesName, value));
        }


        // Use ZView to log data to be printed but not plotted.
        public static void ZLog(DateTimeOffset dto, string SeriesName, decimal value)
        {
            ZLog(dto, SeriesName, (double)value);
        }


        // Use ZView to log data to be printed but not plotted.
        public static void ZLog(string message)
        {
            Trace.WriteLine(message);
        }


        public static string Truncate(this string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
