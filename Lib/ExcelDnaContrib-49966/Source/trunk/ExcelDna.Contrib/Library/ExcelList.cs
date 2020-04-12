using System;
using System.Collections.Generic;
using ExcelDna.Integration;

namespace ExcelDna.Contrib.Library
{
    /// <summary>
    /// Provides functionality to work on a List and provide output for use with ExcelDNA
    /// </summary>
    /// <typeparam name="T">Type of Data to store</typeparam>
    public class ExcelList : List<object>
    {
        /// <summary>
        /// Override for ToArray to ensure a 2D array is returned
        /// </summary>
        /// <returns>2D array compatible with ExcelDna</returns>
        public new object[,] ToArray()
        {
            object[,] ret = new object[base.Count,2];
            for (int i = 0; i < base.Count; i++)
            {
                ret[i, 0] = base[i];
            }
            return ret;
        }
    }
}
