using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using ExcelDna.Integration;


namespace XL
{
    public class XLContraModusFunctions
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Creates a basket expression out of BBG tickers")]
        public static object Basket(object[] Tickers, object[] WeightsOpt, object[] IROpt, object OmitZeroWeightsOpt, object CumProdOpt)
        {
            string[] tickers = Utils.GetVector<string>(Tickers);
            object[] weightsOpt = Utils.GetOptionalParameter<object[]>(WeightsOpt, null);
            bool[] ir = Utils.GetVector<bool>(IROpt);
            bool cumProd = Utils.GetOptionalParameter(CumProdOpt, false);

            bool omitZeroWeights = Utils.GetOptionalParameter(OmitZeroWeightsOpt, false);

            double[] Weights;
            if ((weightsOpt == null || weightsOpt[0] == ExcelMissing.Value) && omitZeroWeights == false)
            {
                Weights = new double[tickers.Length];
                for (int i = 0; i < Weights.Length; ++i)
                    Weights[i] = 1;
            }
            else
            {
                double[] weights = Utils.GetVector<double>(weightsOpt);
                Weights = weights.Select(x => (double)x).ToArray();
                if (tickers.Length != Weights.Length)
                {
                    throw new Exception("Error, inconsistent parameters!");
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tickers.Length; ++i)
            {
                if (omitZeroWeights && Weights[i] == 0)
                    continue;

                if (i > 0)
                    sb.Append("+");

                if (Weights[i] < 0)
                {
                    sb.Append("(0 " + string.Format("{0:+0.000;-0.000}", Weights[i]) + ")");
                }
                else
                {
                    sb.Append(string.Format("{0:0.000}", Weights[i]));
                }

                bool isIR = (ir.Length > 0 ? ir[i] : false);

                sb.Append(" * " + (isIR ? "({d@" : "{r@") + tickers[i].ToString() + "}" + (isIR ? ")" : ""));
            }

            if (cumProd)
                return CumProd(new object[]{sb.ToString()}, new object[]{1.0}, ExcelMissing.Value);

            return sb.ToString();
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Creates an expression out of 2 sub-expressions.")]
        public static object CumProd(object[] Expressions, object[] WeightsOpt, object OverallScaleOpt)
        {
            string[] expressions = Utils.GetVector<string>(Expressions);
            object[] weightsOpt = Utils.GetOptionalParameter<object[]>(WeightsOpt, null);

            double[] Weights;
            if ((weightsOpt == null || weightsOpt[0] == ExcelMissing.Value))
            {
                Weights = new double[expressions.Length];
                for (int i = 0; i < Weights.Length; ++i)
                    Weights[i] = 1;
            }
            else
            {
                double[] weights = Utils.GetVector<double>(weightsOpt);
                Weights = weights.Select(x => (double)x).ToArray();
                if (expressions.Length != Weights.Length)
                {
                    throw new Exception("Error, inconsistent parameters!");
                }
            }

            StringBuilder sb = new StringBuilder();
            if (Weights[0] < 0)
                sb.Append("(0 - " + Weights[0].ToString("{0:0.00000}") + " * (" + expressions[0] + ")");
            else
                sb.Append(string.Format("{0:0.00000} * ({1})", Weights[0], expressions[0]));

            for (int i = 1; i < expressions.Length; ++i)
            {
                sb.Append(string.Format("{0:+0.00000;-0.00000} * ({1})", Weights[i], expressions[i]));
            }

            double overallScale = Utils.GetOptionalParameter(OverallScaleOpt, 1.0);
            return string.Format("cumprod({0} * ({1}) + 1)", overallScale, sb.ToString());
        }
    }
}
