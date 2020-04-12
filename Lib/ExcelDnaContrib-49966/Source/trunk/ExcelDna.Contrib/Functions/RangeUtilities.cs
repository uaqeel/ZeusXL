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

using ExcelDna.Contrib.Library;
using ExcelDna.Integration;

namespace ExcelDna.Contrib
{
    public class RangeUtilities
    {
        private const string CATEGORY = "ExcelDna.Contrib.RangeUtilities";

        /// <summary>
        /// Check if the top left cell of a Range contains a formula
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>True if the top left cell contains a forumla</returns>
        [ExcelFunction(Description="Returns True if top left cell of Range is a formula", IsMacroType=true,Category=CATEGORY,IsVolatile=true)]
        public static bool IsFormula([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return false;
            }
            else
            {
                ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
                return rng.HasFormula;
            }
        }

        /// <summary>
        /// Get the number format of the top left cell of the Range
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>The number format</returns>
        [ExcelFunction(Description = "Returns the number format of the top left cell of the range", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static string GetNumberFormat([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return "Not an Excel Range";
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.NumberFormat;
        }

        /// <summary>
        /// Check if the top left cell of a Range has a comment
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>True if the top left cell has a comment</returns>
        [ExcelFunction(Description = "Returns true if the top left cell has a comment", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static bool HasComment([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return false;
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.HasComment;
        }

        /// <summary>
        /// Check if the top left cell of a Range is part of an Array Formula
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>True if the top left cell is part of an Array Formula</returns>
        [ExcelFunction(Description = "Returns true if the top left cell is part of an Array Formula", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static bool IsArrayFormula([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return false;
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.IsInArrayFormula;
        }

        /// <summary>
        /// Convert the value of the top left cell to text
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>The text of the top left cell</returns>
        [ExcelFunction(Description = "Converts the value of the top left cell to text", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static string RangeAsText([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return "Not a range";
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.ValueAsText;
        }

        /// <summary>
        /// Check if the top left cell of a Range is locked
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>True if the top left cell is locked</returns>
        [ExcelFunction(Description = "Returns true if the top left cell is locked", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static bool IsLocked([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return false;
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.IsLocked;
        }

        /// <summary>
        /// Get the formula of the top left cell
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>The formula of the top left cell</returns>
        [ExcelFunction(Description = "Returns the formula of the top left cell", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static string GetFormula([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return "Not a Range";
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);
            return rng.Formula;
        }

        /// <summary>
        /// Get the Address of the top left cell
        /// </summary>
        /// <param name="TheRange">The Range to check</param>
        /// <returns>The Address of the top left cell</returns>
        [ExcelFunction(Description = "Returns the address of the given range", IsMacroType = true, Category = CATEGORY, IsVolatile = true)]
        public static string GetAddress([ExcelArgument(AllowReference = true, Description = "Range to check")] object TheRange, [ExcelArgument(AllowReference = false, Description = "Specify true if you wish to return the address in External format (E.g. [WorkbookName]SheetName!A1:C1)")] bool External, [ExcelArgument(Description = "Specify true if you wish to return the address in absolute style (E.g. $A$1:$C$1)")]bool FixedStyle, [ExcelArgument(Description = "Specify true if you wish to return the address in R1C1 Style)")] bool R1C1Style)
        {
            if (!Utilities.IsExcelReference(TheRange))
            {
                return "Not a Range";
            }

            ExcelRange rng = new ExcelRange(TheRange as ExcelReference);

            return rng.Address(External,FixedStyle,R1C1Style);
            
        }
    }
}
