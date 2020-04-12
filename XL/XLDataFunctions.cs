using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.IO;
using System.Diagnostics;

using ExcelDna.Integration;
using CommonTypes;


namespace XL
{
    public partial class XLDataFunctions
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Return a submatrix of the input matrix")]
        public static object GetSubMatrix(object[,] Data, int NumRows, int NumCols, object StartRowOpt, object StartColOpt)
        {
            object[,] data = Utils.GetMatrix<object>(Data);
            int nRows = data.GetLength(0), nCols = data.GetLength(1);

            int startRow = Utils.GetOptionalParameter(StartRowOpt, 0);
            int startCol = Utils.GetOptionalParameter(StartColOpt, 0);

            NumRows = Math.Min(NumRows, nRows - startRow);
            NumCols = Math.Min(NumCols, nCols - startCol);

            object[,] data2 = new object[NumRows, NumCols];
            for (int i = 0; i < NumRows; ++i)
            {
                for (int j = 0; j < NumCols; ++j)
                {
                    data2[i, j] = data[startRow + i, startCol + j];
                }
            }

            return data2;
        }
    }
}
