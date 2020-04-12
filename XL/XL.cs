using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using ExcelDna.Contrib.Library;
using XL.Properties;
using CommonTypes;
using DataSources;
using Strategies;


namespace XL
{
    [ComVisible(true)]
    public partial class XL : /*ExcelRibbon,*/ IExcelAddIn
    {
        public static Configuration Config;

        public static int EpochSecs;
        public static string ZEvalScript;
        public static Dictionary<string, Type> DataSourceTypes;


        static XL()
        {
            Config = ConfigurationManager.OpenExeConfiguration(typeof(XL).Assembly.Location);
            EpochSecs = Int32.Parse(Config.AppSettings.Settings["EpochSecs"].Value);
            ZEvalScript = Config.AppSettings.Settings["ZEvalScriptPath"].Value;
            XLOM.SerialiseLocation = Config.AppSettings.Settings["XLOMDataStoreLocation"].Value;

            // In case we're on German windows...
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-us");
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "&Tidy Array", ShortCut = "^+T", Name = "TidyArray")]
        public static void TidyArray()
        {
            ExcelRange selection = new ExcelRange(XlCall.Excel(XlCall.xlfSelection, new object[0]) as ExcelReference);
            if (selection.HasFormula)
            {
                string formula = selection.Formula;
                //string newFormula = Regex.Replace(formula, "([A-Z]{1,2}[0-9]+)", "!$1");
                string newFormula = Regex.Replace(formula, @"(\${0,1}[A-Z]{1,2}\${0,1}[0-9]+)", "!$1");
                newFormula = Regex.Replace(newFormula, "!!", "!");

                object value = XlCall.Excel(XlCall.xlfEvaluate, newFormula);
                if (value is ExcelDna.Integration.ExcelError || value is ExcelDna.Integration.ExcelReference ||
                    (value.GetType().IsArray && ((object[,])value)[0, 0] is ExcelDna.Integration.ExcelError))
                    value = XlCall.Excel(XlCall.xlfEvaluate, formula);

                if (value is object[,])
                {
                    object[,] values = value as object[,];
                    int nRows = values.GetLength(0), nCols = values.GetLength(1);

                    ExcelReference newRef = new ExcelReference(selection.AsRef.RowFirst, selection.AsRef.RowFirst + nRows - 1,
                                                               selection.AsRef.ColumnFirst, selection.AsRef.ColumnFirst + nCols - 1);
                    XlCall.Excel(XlCall.xlcSelect, newRef);
                }
            }
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "&Clear Memory", Name = "ClearMemory")]
        public static void ClearMemory()
        {
            XLOM.Reset();
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "&Clear Memory && Recalculate", Name = "ClearMemoryAndRecalculate")]
        public static void ClearMemoryAndRecalculate()
        {
            XLOM.Reset();
            XlCall.Excel(XlCall.xlcCalculateNow);
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Calculate Selected &Range", ShortCut = "^+R", Name = "CalculateRange")]
        public static void CalculateRange()
        {
            TidyArray();

            dynamic app = ExcelDnaUtil.Application;
            app.Selection.Calculate();
        }


        [ExcelCommand(ShortCut = "^+=")]
        public static void SecondShortcutToCalculateRange()
        {
            CalculateRange();
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Show &Log Window", Name = "ShowLog")]
        public static void ShowLog()
        {
            ExcelDna.Logging.LogDisplay.Show();
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Open &Zeval Script", Name = "OpenZEvalScript")]
        public static void OpenZEvalScript()
        {
            Process.Start(ZEvalScript);
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Show &Object List", Name = "DisplayObjectList")]
        public static void DisplayObjectList()
        {
            ObjectList ol = new ObjectList();
            ol.Show();
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Save XLOM", Name = "XLOMSave")]
        public static void XLOMSave()
        {
            XLOM.Serialise();
        }


        [ExcelCommand(MenuName = "&Zeus", MenuText = "Load XLOM", Name = "XLOMLoad")]
        public static void XLOMLoad()
        {
            XLOM.Deserialise();
        }


        // Required by ExcelDna.
        public void AutoOpen()
        {
        }


        // Required by ExcelDna.
        public void AutoClose()
        {
        }
    }


    [ComVisible(true)]
    public class ZeusRibbon : ExcelRibbon
    {
    }
}
