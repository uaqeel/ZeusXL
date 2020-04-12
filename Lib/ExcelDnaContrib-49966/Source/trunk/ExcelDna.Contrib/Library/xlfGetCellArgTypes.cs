namespace ExcelDna.Contrib.Library
{
    /// <summary>
    /// NB: This enumeration does not currently contain all possible values for the XLCall.Excel function and not all of the ones covered have been implemented yet
    /// </summary>
    public enum xlfGetCellArgTypes
    {
        AbsoluteReference = 1,
        Value = 5,
        Formula = 6,
        NumberFormat = 7,
        IsLocked = 14,
        IsFormulaHidden = 15,
        ColumnInfo = 16,
        RowHeight = 17,
        ContainingWorkbookAndSheet = 32,
        RawFormula = 41,
        HasTextNote = 46,
        HasFormula = 48,
        HasArrayFormula = 49,
        IsStringConstant = 52,
        AsText = 53,
        CurrentWorkBookAndWorksheet = 62,
        WorkbookName = 66
    }
}
