/*
  Copyright (C) 2009 Hayden Smith

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Hayden Smith
  hayden.smith@gmail.com
*/

using System.Configuration;
using ExcelDna.Integration;

namespace ExcelDna.Contrib.Library
{
    public class Utilities
    {
        /// <summary>
        /// Gets the caller of the function
        /// </summary>
        /// <returns>Caller of function as ExcelReference</returns>
        public static ExcelReference Caller()
        {
            return XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
        }

        /// <summary>
        /// Check if a reference is an Excel Reference
        /// </summary>
        /// <param name="input">The reference to check</param>
        /// <returns>True if the supplied reference is an Excel Reference</returns>
        public static bool IsExcelReference(object input)
        {
            return input is ExcelReference;
        }

        /// <summary>
        /// Calls through to ExcelDna wrapper to Excel API
        /// Used to obtain information about a top left cell of range
        /// </summary>
        /// <param name="reference">Range/Cell to query (Only top left cell or a range is queried)</param>
        /// <param name="info">Information to query </param>
        public static object GetCell(ExcelReference reference, xlfGetCellArgTypes info)
        {
            return XlCall.Excel(XlCall.xlfGetCell, (int)info, reference);
        }

        /// <summary>
        /// Determines whether the given object is an Empty Excel cell
        /// </summary>
        /// <param name="input">object to test</param>
        public static bool IsEmpty(object input)
        {
            return input is ExcelEmpty;
        }

        /// <summary>
        /// Determines whether the given object is an Empty Excel cell or is missing
        /// </summary>
        /// <param name="input">object to test</param>
        public static bool IsMissingOrEmpty(object input)
        {
            return IsMissing(input) || IsEmpty(input);
        }

        /// <summary>
        /// Determines whether the given object is missing 
        /// </summary>
        /// <param name="input">object to test</param>
        public static bool IsMissing(object input)
        {
            return input is ExcelMissing;
        }

        /// <summary>
        /// Determines whether any of the given objects of a 1D array are missing 
        /// </summary>
        /// <param name="inputs">1D object array to test</param>
        public static bool IsMissing(object[] inputs)
        {
            for (var i = 0; i < inputs.Length; i++)
                if (inputs[i] == null || IsMissing(inputs[i]))
                    return true;

            return false;
        }

        /// <summary>
        /// Determines whether any of the given objects of a 2D array are missing 
        /// </summary>
        /// <param name="inputs">2D object array to test</param>
        public static bool IsMissing(object[,] inputs)
        {
            for (int i1 = 0; i1 < inputs.GetLength(0); i1++)
                for (int i2 = 0; i2 < inputs.GetLength(1); i2++)
                    if (inputs[i1, i2] == null || IsMissing(inputs[i1, i2]))
                        return true;

            return false;
        }

        /// <summary>
        /// Gets an application setting from the config file for the assembly that this code is in
        /// </summary>
        /// <param name="key">The key of the value to return</param>
        /// <returns>The value associated with the supplied key</returns>
        internal static string GetApplicationSetting(string key)
        {
            Configuration c = ConfigurationManager.OpenExeConfiguration(typeof(Utilities).Assembly.Location);

            if (c.HasFile)
            {
                return c.AppSettings.Settings[key].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Prepends the current location of the assembly that this code is in to the supplied file name
        /// </summary>
        /// <param name="fileName">The filename to use</param>
        /// <returns>A fully qualified file name</returns>
        internal static string PrependAssemblyPath(string fileName)
        {
            if (fileName.Contains(":")) return fileName; // already a full path to file name
            return (typeof(Utilities)).Assembly.Location.Replace("ExcelDna.Contrib.dll", fileName);
        }
    }
}
