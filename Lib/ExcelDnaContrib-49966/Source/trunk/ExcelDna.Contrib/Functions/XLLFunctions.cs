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

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelDna.Integration;

namespace ExcelDna.Contrib.Functions
{
    /// <summary>
    /// Functions to provide information about the XLL
    /// </summary>
    public class XLLFunctions
    {
        private const string CATEGORY = "ExcelDna.Contrib.Info";

        /// <summary>
        /// Get the name of the xll the add-in is running in
        /// </summary>
        [ExcelFunction(Category = CATEGORY, Description = "Returns the Name of the ExcelDna XLL", IsVolatile = true, IsMacroType = true, IsThreadSafe = true)]
        public static string ExcelDnaFileName()
        {
            return XlCall.Excel(XlCall.xlGetName) as string;
        }

        /// <summary>
        /// Get the version of the ExcelDna xll
        /// </summary>
        [ExcelFunction(Category = CATEGORY, Description = "Returns version number of ExcelDna XLL", IsVolatile = true, IsMacroType = true, IsThreadSafe = true)]
        public static string ExcelDnaVersion()
        {
            FileVersionInfo info;
            string filename = ExcelDnaFileName();
            info = FileVersionInfo.GetVersionInfo(filename);
            return info.ProductVersion;
            
        }
    }
}
