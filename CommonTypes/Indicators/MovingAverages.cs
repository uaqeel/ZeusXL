using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using CommonTypes;


// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages
namespace CommonTypes
{
    public class SMA : Indicator
    {
        int WindowLength;
        CircularBuffer<double> Data;

        public double Average { get; set; }


        public SMA(int windowLength)
        {
            WindowLength = windowLength;
            Data = new CircularBuffer<double>(WindowLength);

            Average = 0;
        }


        public SMA(Dictionary<string, object> config)
            : this(config.GetOrDefault("WindowLength", 20.0).AsInt())
        {
        }


        public override double Update(double Value)
        {
            if (Data.Length == 0 && Average != 0)
            {
                for (int i = 0; i < WindowLength; ++i)
                    Data.Insert(Average);
            } else if (Average == 0) {
                Average = Value;
            }

            //Average += (Data.Length == WindowLength ? -Data[0] / (Data.Length) : 0);
            Average += (Value - (Data.Length == WindowLength ? Data[0] : Average)) / (Data.Length + (Data.Length == WindowLength ? 0 : 1));

            Data.Insert(Value);

            return Average;
        }


        public override double Value
        {
            get { return Average; }
        }
    }


    // EMA_t = Value * lambda + EMA_{t-1} * (1 - lambda)
    //       = EMA_{t-1} + lambda * (Value - EMA_{t-1}).
    //
    // Rough SMA-to-EMA conversion:
    //      SMA of window length N = EMA with lambda = 2 / (N + 1).
    public class EMA : SMA
    {
        double SmoothingFactor;


        // smoothingFactor == 0     =>      Value = first value (constant)
        // smoothingFactor == 1     =>      Value = last value (no memory)
        public EMA(double smoothingFactor) : base(0)
        {
            SmoothingFactor = smoothingFactor;
            Average = -double.MaxValue;
        }


        public EMA(int nDays) : this(2.0 / (nDays + 1))
        {
        }


        public EMA(Dictionary<string, object> config)
            : this(config.GetOrDefault("SmoothingFactor", 0.5).AsDouble())
        {
        }


        public override double Update(double Value)
        {
            if (Average == -double.MaxValue)
            {
                Average = Value;
            }
            else
                Average += SmoothingFactor * (Value - Average);

            return Average;
        }


        public override double Value
        {
            get { return Average; }
        }
    }


    // http://www.etrading.sk/en/technical-analysis/44-indikatory-technickej-analyzy/152-kama-kaufman-adaptive-moving-average
    public class KAMA : Indicator
    {
        double SmoothingLowerLimit, SmoothingUpperLimit;
        int WindowLength;

        CircularBuffer<double> Data;

        double Average, Vol, ER;                                // Average == KAMA_t.


        public KAMA(double smoothingLowerLimit, double smoothingUpperLimit, int windowLength)
        {
            SmoothingLowerLimit = (1 - smoothingLowerLimit);
            SmoothingUpperLimit = Math.Min(1 - smoothingUpperLimit, SmoothingLowerLimit);
            WindowLength = windowLength;

            Data = new CircularBuffer<double>(WindowLength);
            Average = Vol = ER = 0;
        }


        public KAMA(Dictionary<string, object> config)
            : this(config.GetOrDefault("SmoothingLowerLimit", 0.5).AsDouble(),
                   config.GetOrDefault("SmoothingUpperLimit", 1.0).AsDouble(),
                   config.GetOrDefault("WindowLength", 20.0).AsInt())
        {
        }


        public override double Update(double Value)
        {
            // Vol is going to be the sum of absolute differences over the window, so we
            // take the first value off before inserting modifying the window.
            if (Data.Length == WindowLength)
                Vol -= Math.Abs(Data[1] - Data[0]);

            Data.Insert(Value);

            if (Data.Length == 1)
            {
                Average = Value;
            }
            else
            {
                Vol += Math.Abs(Data.Last() - Data.FromLast(-1));
                ER = Math.Abs(Data.Last() - Data[0]) / Math.Max(1e-5, Vol);

                // The smoothing constant we're going to use for the EMA is an interpolated value between
                // the fast and slow smoothing based on the efficiency ratio above.
                double smoothingConstant = ER * (SmoothingUpperLimit - SmoothingLowerLimit) + SmoothingLowerLimit;

                // Shrink the smoothing constant to smooth further.
                smoothingConstant *= smoothingConstant;

                Average += smoothingConstant * (Value - Average);
            }

            return Average;
        }


        public override double Value
        {
            get { return Average; }
        }
    }
}
