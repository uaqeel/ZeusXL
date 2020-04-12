using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonTypes.Maths
{
    public class Regression
    {
        public double Slope;
        public double Intercept;
        public double CoefficientOfDetermination;
        public double Correlation;
        public double StandardError;


        public Regression(List<double> X, List<double> Y)
        {
            ///Variable declarations            
            int num = 0; //use for List count
            double sumX = 0; //summation of x[i]
            double sumY = 0; //summation of y[i]
            double sum2X = 0; // summation of x[i]*x[i]
            double sum2Y = 0; // summation  of y[i]*y[i]
            double sumXY = 0; // summation of x[i] * y[i]  
            double denX = 0;
            double denY = 0;
            double top = 0;
            double correlation = 0; // holds correlation
            double slope = 0; // holds slope(beta)
            double y_intercept = 0; //holds y-intercept (alpha)

            //Standard error variables
            double sum_res = 0.0;
            double yhat = 0;
            double res = 0;
            double standardError = 0; //
            int n = 0;
            //End standard variable declaration
            //End variable declaration

            #region Computation begins

            num = X.Count;  //Since the X and Y list are of same length, so 
            // we can take the count of any one list 
            sumX = X.Sum();  //Get Sum of X list
            sumY = Y.Sum(); //Get Sum of Y list           
            X.ForEach(i => { sum2X += i * i; }); //Get sum of x[i]*x[i]           
            Y.ForEach(i => { sum2Y += i * i; }); //Get sum of y[i]*y[i]            
            sumXY = Enumerable.Range(0, num).Select(i => X[i] * Y[i]).Sum();//Get Summation of x[i] * y[i]

            //Find denx, deny,top
            denX = num * sum2X - sumX * sumX;
            denY = num * sum2Y - sumY * sumY;
            top = num * sumXY - sumX * sumY;

            //Find correlation, slope and y-intercept
            correlation = top / Math.Sqrt(denX * denY);
            slope = top / denX;
            y_intercept = (sumY - sumX * slope) / num;


            //Implementation of Standard Error
            sum_res = Enumerable.Range(0, num).Aggregate(0.0, (sum, i) =>
            {
                yhat = y_intercept + (slope * X[i]);
                res = yhat - Y[i];
                n++;
                return sum + res * res;
            });

            if (n > 2)
            {
                standardError = sum_res / (1.0 * n - 2.0);
                standardError = Math.Pow(standardError, 0.5);
            }
            else standardError = 0;

            #endregion

            //Add the computed value to the resultant dictionary
            Slope = slope;
            Intercept = y_intercept;
            Correlation = correlation;
            StandardError = standardError;
            CoefficientOfDetermination = correlation * correlation;
        }
    }
}
