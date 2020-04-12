using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using CommonTypes;


namespace CommonTypes
{
    // http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:average_true_range_a
    public class ATR : Indicator
    {
        public int WindowLength;
        public CircularBuffer<Tuple<double, double, double>> Cache;            // close, low, high

        public EMA ATRValue;


        public ATR(int windowLength)
        {
            WindowLength = windowLength;
            if (WindowLength < 3)
                throw new Exception("Error, ATR calculation requires at least 2 full bars! (WindowLength must be greater than 2)");

            Cache = new CircularBuffer<Tuple<double, double, double>>(WindowLength);

            ATRValue = new EMA(1D / WindowLength);
            ATRValue.Update(0);
        }


        public ATR(Dictionary<string, object> config)
            : this(config.GetOrDefault("WindowLength", 20.0).AsInt())
        {
        }


        public override double Update(double newValue)
        {
            throw new Exception("Error, Average True Range calculation needs more data!");
        }


        public double Update(double newClose, double newLow, double newHigh)
        {
            Cache.Insert(new Tuple<double, double, double>(newClose, newLow, newHigh));

            if (Cache.Length == WindowLength) {
                double highLowRange = newHigh - newLow;

                double TR = highLowRange;
                Tuple<double, double, double> previous = Cache.FromLast(-1);
                double previousClose = previous.Item1;
                double previousLow = previous.Item2;
                double previousHigh = previous.Item3;

                double highCloseRange = Math.Abs(newHigh - previousClose);
                double lowCloseRange = Math.Abs(newLow - previousClose);

                TR = Math.Max(highLowRange, Math.Max(highCloseRange, lowCloseRange));
                ATRValue.Update(TR);
            }

            return Value;
        }


        public override double Update(Bar bar)
        {
            return Update((double)bar.Close, (double)bar.Low, (double)bar.High);
        }


        public override double Value
        {
            get
            {
                return ATRValue.Value;
            }
        }
    }


    // RV is robust for sampling 10-30m return data (http://www.oxford-man.ox.ac.uk/~nshephard/papers/ecta08.pdf).
    public class Statistics : Indicator
    {
        int WindowLength;
        CircularBuffer<double> Cache;

        public double[] Stats;                                              // Count, Min, Max, Average, Stdev, Total.


        public Statistics(int windowLength)
        {
            WindowLength = windowLength;
            Cache = new CircularBuffer<double>(WindowLength);

            Stats = new double[6];
        }


        public Statistics(Dictionary<string, object> config)
            : this(config.GetOrDefault("WindowLength", 20.0).AsInt())
        {
        }


        public override double Update(double newValue)
        {
            Cache.Insert(newValue);
            if (Cache.Length == WindowLength)
                Stats = Cache.Statistics();

            return Value;
        }


        public override double Value
        {
            get
            {
                return Stats[4];                                            // Vol.
            }
        }
    }


    public class EWVol : Indicator
    {
        double Vol2;
        double DecayFactor;
        int N;

        int n;

        public EWVol(double decayFactor)
        {
            DecayFactor = decayFactor;
            N = (int)(2.0 / (DecayFactor + 1));
            n = 0;
        }


        public override double Update(double newReturn)
        {
            if (n < N)
            {
                Vol2 += newReturn * newReturn / N;
                n++;

                return 0;
            }
            else
            {
                Vol2 = DecayFactor * Vol2 + (1 - DecayFactor) * newReturn * newReturn;
                return Math.Sqrt(Vol2);
            }
        }


        public override double Value
        {
            get { return Math.Sqrt(Vol2); }
        }
    }
}
