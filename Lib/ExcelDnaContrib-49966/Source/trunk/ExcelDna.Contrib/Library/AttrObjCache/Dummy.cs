using System;

using ExcelDna.Integration;

// this class is for test purposes only
// see ExcelDna.Contrib.xls in the Spreadsheets folder
namespace ExcelDna.Contrib.Cache
{
    /// <summary>
    /// Test class for object cache
    /// </summary>
    [ExcelObject(Name = "Dummy", Description = "Dummy test class")]
    public class Dummy
    {
        private int _df2 = 123456;
        private int[] _df3;
        private string[,] _df4;
        private double[,] _df5;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="df3"></param>
        [ExcelObjectConstructor("DF3", "Initialize int[] DF3")]
        public Dummy(int[] df3)
        {
            _df3 = df3;
            _df4 = new string[,] { { "a", "b" }, { "c", "d" } };
            _df5 = new double[,] { { -1, -2 }, { -3, -4 } };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="df3"></param>
        /// <param name="df4"></param>
        [ExcelObjectConstructor("DF34", "Initialize int[] DF3 and string[,] DF4")]
        public Dummy(int[] df3, string[,] df4)
        {
            _df3 = df3;
            _df4 = df4;
            _df5 = new double[,] { { -1, -2 }, { -3, -4 } };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="df3"></param>
        /// <param name="df5"></param>
        [ExcelObjectConstructor("DF35", "Initialize int[] DF3 and double[,] DF5")]
        public Dummy(int[] df3, double[,] df5)
        {
            _df3 = df3;
            _df5 = df5;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="df5"></param>
        /// <param name="df3"></param>
        [ExcelObjectConstructor("DF53", "Initialize double[,] DF5 and int[] DF3")]
        public Dummy(double[,] df5, int[] df3)
        {
            _df3 = df3;
            _df5 = df5;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dummy()
        {
            _df3 = new int[] { 12, 5 };
            _df4 = new string[,] { { "a", "b" }, { "c", "d" } };
            _df5 = new double[,] { { -1, -2 }, { -3, -4 } };
        }

        /// <summary>
        /// 
        /// </summary>
        [ExcelObjectProperty(Name = "DummyField", Description = "DummyField in test class")]
        public string DummyField { get { return "Dummy"; } }

        /// <summary>
        /// 
        /// </summary>
        [ExcelObjectProperty(Name = "DummyField2", Description = "DummyField2 in test class")]
        public int DummyField2 { get { return _df2; } set { _df2 = value; } }

        /// <summary>
        /// 
        /// </summary>
        [ExcelObjectProperty(Name = "ArrField3", Description = "ArrField3 in test class")]
        public int[] DummyField3 { get { return _df3; } set { _df3 = value; } }

        /// <summary>
        /// 
        /// </summary>
        [ExcelObjectProperty(Name = "ArrField4", Description = "ArrField4 in test class")]
        public string[,] DummyField4 { get { return _df4; } set { _df4 = value; } }

        /// <summary>
        /// 
        /// </summary>
        [ExcelObjectProperty(Name = "ArrField5", Description = "ArrField5 in test class")]
        public double[,] DummyField5 { get { return _df5; } set { _df5 = value; } }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [ExcelObjectMethod(Name = "DummyMethod", Description = "DummyMethod in test class")]
        public bool DummyMethod() { return true; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        [ExcelObjectMethod(Name = "DummyMethod2", Description = "DummyMethod2 in test class")]
        public double DummyMethod2(double[] o)
        {
            double res = 0;
            for (int j = 0; j < o.GetLength(0); j++)
                res += 5 * o[j];

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="o1"></param>
        /// <returns></returns>
        [ExcelObjectMethod(Name = "DummyMethod3", Description = "DummyMethod3 in test class")]
        public double DummyMethod3(double[,] o, double[] o1)
        {
            double res = 0;
            for (int i = 0; i < o.GetLength(0); i++)
                for (int j = 0; j < o.GetLength(1); j++)
                    res += 5 * o[i, j];

            for (int i = 0; i < o1.GetLength(0); i++)
                res += o1[i];

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        [ExcelObjectMethod(Name = "DummyMethod4", Description = "DummyMethod3 in test class")]
        public double DummyMethod4(int[] o)
        {
            double res = 0;
            for (int i = 0; i < o.GetLength(0); i++)
                res += o[i] * o[i];

            return Math.Sqrt(res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        [ExcelObjectMethod(Name = "FOTM", Description = "fifteenth of this month")]
        public DateTime DummyMethod4(DateTime d)
        {
            return new DateTime(d.Year, d.Month, 15);
        }
    }
}
