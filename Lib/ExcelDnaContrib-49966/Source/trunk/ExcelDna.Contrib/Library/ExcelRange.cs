using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelDna.Integration;

namespace ExcelDna.Contrib.Library
{
    public class ExcelRange
    {
        /// <summary>
        /// Excel Reference from which Range information is obatined
        /// </summary>
        private ExcelReference _ref;

        /// <summary>
        /// Returns the Range as an ExcelReference
        /// </summary>
        public ExcelReference AsRef
        {
            get { return _ref; }
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="ExcelRef">ExcelReference from which to construct Range</param>
        public ExcelRange(ExcelReference ExcelRef)
        {
            _ref = ExcelRef;
        }

        /// <summary>
        /// Sets the values of the Range
        /// </summary>
        public object Value
        {
            get { return AsRef.GetValue(); }
            set { AsRef.SetValue(value); }

        }

        /// <summary>
        /// Number of Rows in Range
        /// </summary>
        public int RowCount
        {
            get { return (AsRef.RowLast - AsRef.RowFirst) + 1; }
        }

        /// <summary>
        /// Number of columns in Range
        /// </summary>
        public int ColumnCount
        {
            get { return (AsRef.ColumnLast - AsRef.ColumnFirst) + 1; }
        }

        /// <summary>
        /// Returns the Formula of the top left cell
        /// </summary>
        public string Formula
        {
            get { return Utilities.GetCell(AsRef,xlfGetCellArgTypes.Formula) as string; }
        }

        /// <summary>
        /// Is the top left cell of the Range a formula
        /// </summary>
        public bool HasFormula
        {
            get { return (bool)Utilities.GetCell(AsRef, xlfGetCellArgTypes.HasFormula);}
        }

        /// <summary>
        /// Does the top left cell have a comment
        /// </summary>
        public bool HasComment
        {
            get { return (bool)Utilities.GetCell(AsRef, xlfGetCellArgTypes.HasTextNote); }
        }

        /// <summary>
        /// Is the top left cell of Range locked
        /// </summary>
        public bool IsLocked
        {
            get { return (bool)Utilities.GetCell(AsRef, xlfGetCellArgTypes.IsLocked); }
        }

        /// <summary>
        /// The Number Format of the top left cell
        /// </summary>
        public string NumberFormat
        {
            get { return (string)Utilities.GetCell(AsRef, xlfGetCellArgTypes.NumberFormat); }
        }

        /// <summary>
        /// Is the top left cell part of an array formula
        /// </summary>
        public bool IsInArrayFormula
        {
            get { return (bool)Utilities.GetCell(AsRef, xlfGetCellArgTypes.HasArrayFormula); }
        }

        /// <summary>
        /// Returns the value of the Range as a text strin
        /// </summary>
        public string ValueAsText
        {
            get { return (string)Utilities.GetCell(AsRef, xlfGetCellArgTypes.AsText); }
        }

        /// <summary>
        /// Is the top left formula hidden
        /// </summary>
        public bool IsFormulaHidden
        {
            get { return (bool)Utilities.GetCell(AsRef, xlfGetCellArgTypes.IsFormulaHidden); }
        }

        /// <summary>
        /// Name of workbook that Range is a part of
        /// </summary>
        public string WorkbookName
        {
            get { return (string)Utilities.GetCell(AsRef, xlfGetCellArgTypes.WorkbookName); }
        }

        /// <summary>
        /// Gets the Address in the specified format
        /// </summary>
        /// <param name="External">set to true to return the address in External format (E.g. [Workbookname]Worksheet!A1:C3)</param>
        /// <param name="Fixed">set to true to return the address in fixed format (E.g. $A$1)</param>
        /// <param name="R1C1">set to true to return the address in R1C1 otherwise withh be returned in absolute format</param>
        /// <returns></returns>
        public string Address(bool External,bool Fixed,bool R1C1)
        {
            string baseAddress = string.Empty;

            if (External)
            {
                baseAddress = ExternalAddress;
            }
            else
            {
                baseAddress = LocalAddress;
            }

            if (Fixed)
            {
                baseAddress.Replace("$", "");
            }

            if(R1C1)
            {
                try
                {
                    baseAddress = (string)XlCall.Excel(XlCall.xlfFormulaConvert, baseAddress, true,Type.Missing);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }

            }

            return baseAddress;

        }

        /// <summary>
        /// Get the address in External format ([Workbookname]Worksheet!A1:C3 format) of the range
        /// </summary>
        public string ExternalAddress
        {
            get
            {
                if (SingleCell)
                {
                    return (string)Utilities.GetCell(AsRef, xlfGetCellArgTypes.AbsoluteReference);
                }
                else
                {
                    ExcelRange topLeftRef = new ExcelRange(new ExcelReference(AsRef.RowFirst, AsRef.RowFirst, AsRef.ColumnFirst, AsRef.ColumnFirst, AsRef.SheetId));
                    ExcelRange bottomRightRef = new ExcelRange(new ExcelReference(AsRef.RowLast, AsRef.RowLast, AsRef.ColumnLast, AsRef.ColumnLast, AsRef.SheetId));

                    return topLeftRef.ExternalAddress + ":" + bottomRightRef.LocalAddress;
                }
            }
        }

        /// <summary>
        /// Get the Local address (A1:C3 format) of the range
        /// </summary>
        public string LocalAddress
        {
            get
            {
                string absAddress = string.Empty;

                if (SingleCell)
                {
                    absAddress = (string)Utilities.GetCell(AsRef, xlfGetCellArgTypes.AbsoluteReference);
                    int exclIndex = absAddress.IndexOf("!")+1;

                    return absAddress.Substring(exclIndex);
                }
                else
                {
                    ExcelRange topLeftRef = new ExcelRange(new ExcelReference(AsRef.RowFirst, AsRef.RowFirst, AsRef.ColumnFirst, AsRef.ColumnFirst, AsRef.SheetId));
                    ExcelRange bottomRightRef = new ExcelRange (new ExcelReference(AsRef.RowLast, AsRef.RowLast, AsRef.ColumnLast, AsRef.ColumnLast, AsRef.SheetId));

                    return topLeftRef.LocalAddress + ":" + bottomRightRef.LocalAddress;
                }                
            }
        }

        /// <summary>
        /// Returns the Top Left cell as an ExcelRange
        /// </summary>
        public ExcelRange TopLeft
        {
            get
            {
                ExcelReference theRef = new ExcelReference(AsRef.RowFirst, AsRef.RowFirst, AsRef.ColumnFirst, AsRef.ColumnFirst, AsRef.SheetId);

                return new ExcelRange(theRef);
            }
        }

        /// <summary>
        /// Returns the Bottom Right cell as an ExcelRange
        /// </summary>
        public ExcelRange BottomRight
        {
            get
            {
                ExcelReference theRef = new ExcelReference(AsRef.RowLast, AsRef.RowLast, AsRef.ColumnLast, AsRef.ColumnLast, AsRef.SheetId);

                return new ExcelRange(theRef);
            }
        }

        /// <summary>
        /// Returns true if this range is a single cell
        /// </summary>
        public bool SingleCell
        {
            get { return RowCount == 1 && ColumnCount == 1; }
        }
    }
}
