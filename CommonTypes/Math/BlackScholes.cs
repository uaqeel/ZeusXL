using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using QLNet;


namespace CommonTypes.Maths
{
    public class BlackScholes
    {
        List<BlackScholesCalculator> Option = new List<BlackScholesCalculator>();
        CallPut ActualType;

        Option.Type Type;
        double T;
        double DF;
        double _Vol;
        double GrowthFactor;
        double _Spot;
        double Strike;


        public BlackScholes(CallPut type, double t, double df, double vol, double divYield, double spot, double strike)
        {
            Option.Type tt = QLNet.Option.Type.Call;
            if (type == CallPut.Put)
                tt = QLNet.Option.Type.Put;

            T = Math.Max(0, t);

            double growthFactor = Math.Exp(-divYield * T);

            ActualType = type;

            Type = tt;
            DF = df;
            _Vol = vol;
            GrowthFactor = growthFactor;
            _Spot = spot;
            Strike = strike;

            vol *= Math.Sqrt(T);

            Option.Add(new BlackScholesCalculator(new PlainVanillaPayoff(tt, Strike), spot, growthFactor, vol, df));
            if (ActualType == CallPut.Forward)
                Option.Add(new BlackScholesCalculator(new PlainVanillaPayoff(QLNet.Option.Type.Put, Strike), spot, growthFactor, vol, df));
        }


        public double Premium()
        {
            return (Option[0].value() - (ActualType == CallPut.Forward ? Option[1].value() : 0)) / (ActualType == CallPut.Forward ? Strike / DF : Spot);
        }


        public double Delta()
        {
            double delta = (Option[0].delta() + (ActualType == CallPut.Forward ? 0 - Option[1].delta() : 0));
            return (double.IsNaN(delta) ? 0 : delta);
        }


        // For each 1% spot move, Delta_new = Delta_0 + Gamma().
        public double Gamma()
        {
            double gamma = Option[0].gamma() * Spot / 100;
            return (double.IsNaN(gamma) ? 0 : gamma);
        }


        // For 100bp increase in rates, premium = Premium_0 + Rho(days).
        public double Rho(double days)
        {
            return Option[0].rho(days) / Spot / 100;
        }


        // After "days", premium = Premium_0 - Theta(days).
        public double Theta(double days)
        {
            double theta = Option[0].thetaPerDay(days) / Spot;
            return (double.IsNaN(theta) ? 0 : theta);
        }


        // For 100bp increase in vol, premium = Premium_0 + Vega().
        public double Vega()
        {
            double bumpedVol = (_Vol + 0.01) * Math.Sqrt(T);
            BlackScholesCalculator up = new BlackScholesCalculator(new PlainVanillaPayoff(Type, Strike), _Spot, GrowthFactor, bumpedVol, DF);

            double vega = up.value() - Option[0].value();
            return  (double.IsNaN(vega) ? 0 : vega) / Spot;
        }


        public double Spot
        {
            get { return _Spot; }
            set
            {
                _Spot = value;
                Option[0] = new BlackScholesCalculator(new PlainVanillaPayoff(Type, Strike), _Spot, GrowthFactor, _Vol, DF);
                if (ActualType == CallPut.Forward)
                    Option[1] = new BlackScholesCalculator(new PlainVanillaPayoff(QLNet.Option.Type.Put, Strike), _Spot, GrowthFactor, _Vol, DF);
            }
        }


        public double Vol
        {
            get { return _Vol; }
            set
            {
                _Vol = value;
                Option[0] = new BlackScholesCalculator(new PlainVanillaPayoff(Type, Strike), _Spot, GrowthFactor, _Vol, DF);
                if (ActualType == CallPut.Forward)
                    Option[1] = new BlackScholesCalculator(new PlainVanillaPayoff(QLNet.Option.Type.Put, Strike), _Spot, GrowthFactor, _Vol, DF);
            }
        }
    }


    public enum CallPut {
        Call = 1,
        Put = -1,
        Forward = 0
    }
}
