using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using QLNet;


namespace CommonTypes.Maths
{
    public class ScenarioCube
    {
        List<StochasticProcess1D> Processes;
        Matrix Correlations;

        int nAssets;
        int nSteps;
        double dt;

        Handle<YieldTermStructure> yc;
        StochasticProcessArray spa;
        RandomSequenceGenerator<MersenneTwisterUniformRng> rsg;
        TimeGrid tg;
        MultiPathGenerator<InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>> mpg;


        public ScenarioCube(double r, double[] spots, double[] q, double[] vols, double[,] correlations, int nSteps, double dt)
        {
            nAssets = spots.Length;
            this.nSteps = nSteps;
            this.dt = dt;

            Processes = new List<StochasticProcess1D>(nAssets);
            Correlations = new Matrix(nAssets, nAssets);

            Date now = new Date(DateTime.Now.Date);
            DayCounter dc = new SimpleDayCounter();

            yc = new Handle<YieldTermStructure>(new FlatForward(now, r, dc));
            for (int i = 0; i < nAssets; ++i)
            {
                Handle<Quote> x0 = new Handle<Quote>(new SimpleQuote(spots[i]));
                Handle<YieldTermStructure> dyc = new Handle<YieldTermStructure>(new FlatForward(now, q[i], dc));
                Handle<BlackVolTermStructure> bv = new Handle<BlackVolTermStructure>(new BlackConstantVol(now, new Calendar(), vols[i], dc));

                Processes.Add(new GeneralizedBlackScholesProcess(x0, dyc, yc, bv));

                for (int j = 0; j < nAssets; ++j)
                {
                    if (i == j)
                        Correlations[i, j] = 1;
                    else
                        Correlations[i, j] = Correlations[j, i] = correlations[i, j];
                }
            }

            spa = new StochasticProcessArray(Processes, Correlations);
            rsg = new RandomSequenceGenerator<MersenneTwisterUniformRng>(nSteps * Processes.Count, new MersenneTwisterUniformRng());
            tg = new TimeGrid(nSteps * dt, nSteps);

            var rsg2 = new InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>,
                            InverseCumulativeNormal>(rsg, new InverseCumulativeNormal());

            mpg = new MultiPathGenerator<InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>>(spa, tg, rsg2, false);
        }


        public double[,] Next()
        {
            double[,] ret = new double[nSteps, nAssets + 1];
            
            Sample<IPath> ip = mpg.next();
            MultiPath mp = ip.value as MultiPath;
            for (int i = 0; i < nSteps; ++i)
            {
                for (int j = 0; j < nAssets; ++j)
                {
                    ret[i, j] = mp[j][i];
                }

                ret[i, nAssets] = yc.currentLink().discount(i * dt);
            }

            return ret;
        }
    }
}
