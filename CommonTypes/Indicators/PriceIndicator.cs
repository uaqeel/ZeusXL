using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;

using CommonTypes.Maths;


namespace CommonTypes
{
    // Roughly follows: http://en.wikipedia.org/wiki/MACD
    public class MACD : Indicator
    {
        double FastSmoothingFactor, SlowSmoothingFactor, SignalSmoothingFactor;

        public EMA Fast, Slow, Signal;

        public bool MACDCrossedSignal, MACDCrossedZero, MACDStockDivergence;
        public double MACDValue, SignalValue;

        double maxValue, maxMACDValue;


        public MACD(double fastSmoothingFactor, double slowSmoothingFactor, double signalSmoothingFactor)
        {
            FastSmoothingFactor = fastSmoothingFactor;
            Fast = new EMA(FastSmoothingFactor);

            SlowSmoothingFactor = slowSmoothingFactor;
            Slow = new EMA(SlowSmoothingFactor);

            SignalSmoothingFactor = signalSmoothingFactor;
            Signal = new EMA(SignalSmoothingFactor);

            Signal.Update(0);

            MACDCrossedSignal = false;
            MACDCrossedZero = false;
            MACDStockDivergence = false;

            maxValue = 0;
            maxMACDValue = 0;
        }


        public MACD(Dictionary<string, object> config)
            : this(config.GetOrDefault("FastSmoothingFactor", 0.2).AsDouble(),
                   config.GetOrDefault("SlowSmoothingFactor", 0.5).AsDouble(),
                   config.GetOrDefault("SignalSmoothingFactor", 0.3).AsDouble())
        {
        }


        public override double Update(double newValue)
        {
            double oldMACD = Fast.Value - Slow.Value;
            double oldSignal = Signal.Value;

            Fast.Update(newValue);
            Slow.Update(newValue);

            double newMACD = Fast.Value - Slow.Value;

            Signal.Update(newMACD);
            double newSignal = Signal.Value;

            MACDCrossedSignal = (Math.Sign(oldSignal - oldMACD) != Math.Sign(newSignal - newMACD));
            MACDCrossedZero = (Math.Sign(oldMACD) != Math.Sign(newMACD));
            MACDStockDivergence = Math.Sign(maxValue - newValue) != Math.Sign(maxMACDValue - newMACD);

            if (newValue > maxValue)
                maxValue = newValue;

            if (newMACD > maxMACDValue)
                maxMACDValue = newMACD;

            MACDValue = newMACD;
            SignalValue = newSignal;

            return Value;
        }


        public override double Value
        {
            get
            {
                double MACD = Fast.Value - Slow.Value;
                return (MACD - Signal.Value);                            // This is what's generally referred to as the 'histogram value'.
            }
        }
    }


    // Price action trend indicator, providing HigherHighs and LowerLows.
    public class PriceAction : Indicator
    {
        public int NumData;

        public CircularBuffer<double> Values;

        // Both Highs and Lows are calculated over the values in Values. Note that
        // their window will be much longer than that of Values.
        // TODO(1) - I'm not wild about calculating Highs and Lows over Values, but
        // storing the bar's High and Low.
        public SortedDictionary<double, double> Highs;
        public SortedDictionary<double, double> Lows;

        public bool HigherHighs;
        public bool LowerLows;
        public bool HighVol;

        protected double index;


        public PriceAction(int numData)
        {
            NumData = numData;
            Values = new CircularBuffer<double>(NumData);
            Highs = new SortedDictionary<double, double>();
            Lows = new SortedDictionary<double, double>();

            HigherHighs = false;
            LowerLows = false;
            HighVol = false;

            index = 0;
        }


        public PriceAction(Dictionary<string, object> config)
            : this(config.GetOrDefault("NumData", 100.0).AsInt())
        {
        }


        public override double Update(Bar completeBar)
        {
            return Update((double)completeBar.High, (double)completeBar.Low, (double)completeBar.Close);
        }


        public double Update(double high, double low, double close)
        {
            if (Values.Length > 0)
            {
                bool hh = (high >= Values.Max());
                bool ll = (low <= Values.Min());

                if (hh && ll)
                {
                    HigherHighs = false;
                    LowerLows = false;
                    HighVol = true;

                    // Note, I don't add this point to either Highs or Lows, so it won't be considered in the regression.
                }
                else if (hh)
                {
                    HigherHighs = true;
                    LowerLows = false;
                    HighVol = false;

                    if (Highs.Count >= NumData / 2)
                        Highs.Remove(Highs.First().Key);

                    Highs.Add(index, high);
                }
                else if (ll)
                {
                    LowerLows = true;
                    HigherHighs = false;
                    HighVol = false;

                    if (Lows.Count >= NumData / 2)
                        Lows.Remove(Lows.First().Key);

                    Lows.Add(index, low);
                }
            }

            Values.Insert(close);
            index += 1e-4;

            return Value;
        }


        public override double Value
        {
            get
            {
                return Values.Last();
            }
        }


        // To conform to Indicator, otherwise deprecated.
        public override double Update(double newValue)
        {
            return Update(newValue, newValue, newValue);
        }
    }


    public class PricePattern : PriceAction
    {
        public PricePattern(int numData) : base(numData)
        {
        }


        public PricePattern(Dictionary<string, object> config)
            : this(config.GetOrDefault("NumData", 100.0).AsInt())
        {
        }


        // The products of this function are results of a regression of the extrema and the last data point.
        // They should be treated as separate from the HigherHighs and LowerLows indicators inherited from
        // the PriceAction indicator.
        public Pattern Describe()
        {
            if (!Values.Full)
                return new Pattern();

            List<double> x = Highs.Keys.Union(Lows.Keys).ToList();
            List<double> y = new List<double>(x.Count);
            for (int i = 0; i < x.Count; ++i)
            {
                double yy = 0;
                if (!Highs.TryGetValue(x[i], out yy))
                {
                    yy = Lows[x[i]];
                }
                else
                {
                    yy = Highs[x[i]];
                }

                y.Insert(i, yy);
            }

            // So that we're not just using stale data, eg. after a period of low volatility.
            x.Insert(x.Count, index);
            y.Insert(x.Count - 1, Values.Last());

            Regression rr = new Regression(x, y);

            Pattern ret = new Pattern();

            ret.Slope = rr.Slope;
            ret.R2 = rr.CoefficientOfDetermination;

            ret.Direction = Math.Sign(ret.Slope);
            if (Highs.Count == 0 && Lows.Count > 0)
                ret.DirectionOfLastExtrema = -1;
            else if (Highs.Count > 0 && Lows.Count == 0)
                ret.DirectionOfLastExtrema = 1;
            else if (Highs.Count > 0 && Lows.Count > 0)
                ret.DirectionOfLastExtrema = (Highs.Keys.Last() > Lows.Keys.Last() ? 1 : -1);

            if (ret.Direction == +1)
            {
                ret.UnambiguousTrend = (Values.Last() - Values.First() >= Values.Max() - Values.First());

                if (Highs.Count > 0 && Lows.Count > 0)
                {
                    bool h = (Highs.Values.Last() - Highs.Values.First() >= Highs.Values.Max() - Highs.Values.First());
                    bool l = (Lows.Values.Last() - Lows.Values.First() >= Lows.Values.Max() - Lows.Values.First());

                    ret.UnambiguousTrendOfExtremas = (h == l && h == true);
                }
            }
            else if (ret.Direction == -1)
            {
                ret.UnambiguousTrend = (Values.First() - Values.Last() >= Values.First() - Values.Min());

                if (Highs.Count > 0 && Lows.Count > 0)
                {
                    bool h = (Highs.Values.First() - Highs.Values.Last() >= Highs.Values.First() - Highs.Values.Min());
                    bool l = (Lows.Values.First() - Lows.Values.Last() >= Lows.Values.First() - Lows.Values.Min());

                    ret.UnambiguousTrendOfExtremas = (h == l && h == true);
                }
            }

            return ret;
        }
    }


    public struct Pattern
    {
        // Derived from a regression of the union of Highs, Lows and Values.Last().
        public double Slope;
        public double R2;
        public int Direction;

        // +1 if the last extrema was a high and -1 if the last extrema was a low.
        public int DirectionOfLastExtrema;

        // Fundamental trend indicators: Close - Open > High - Open. Based on
        // Values, which has NumData values and uses the close micro-price.
        public bool UnambiguousTrend;

        // Fundamental trend indicators: Close - Open > High - Open. Based on
        // Highs & Lows (so with a much longer window as UnambiguousTrend).
        // Uses bars' high and low micro-prices.
        public bool UnambiguousTrendOfExtremas;
    }
}
