using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelDna.Contrib.Library
{
    /// <summary>
    /// This enumeration contains the various possible parameters for the xlfFormulaConvert Excel call
    /// </summary>
    public enum xlfFormulaConvertRefType
    {
        RowAndColumnAbsolute=1,
        RowAbsoluteOnly=2,
        ColumnAbsoluteOnly=3,
        RowAndColumnRelative=4
    }
}
