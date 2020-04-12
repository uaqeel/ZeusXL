using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Diagnostics;

using CommonTypes.Maths;


namespace CommonTypes
{
    // This class provides a stochastic-clock counterpart to a deterministic-clock BarList. The clock is specified in terms of
    // the variance of an asset with 1% daily volatily. Thus, setting the clock to 30s is equivalent to specifying bars of length
    //     - 120s for an asset with 0.5% daily volatility (scaling: 0.01^2 * 30 / 86400 / (0.005^2 * 30 / 86400) = 4)
    //     - 30s for an asset with 1% daily volatility
    //     - 7.5s for an asset with 2% daily volatility (scaling: 0.01^2 * 30 / 86400 / (0.02^2 * 30 / 86400) = 0.01^2 / 0.02^2 = 0.25)
    // In addition, we terminate bars when the date changes.
    //
    // Thus, these bars follow the rules below:
    //     1. Variance is measured *within* the bar.
    //     2. Bars are terminated at the earliest instance such that:
    //            - the variance budget is exhausted
    //            - the date changes
    //     3. New bars are created on the tick following the termination of a bar.
    //     4. Gaps are allowed.
    //     5. The resulting distribution of close-on-close returns will be *approximately* heteroskedastic.
    //     6. No bar will be longer than 86400s long.
    //     7. There will be gaps on weekends and holidays.
    //
    // Note that we approximate variance by quadratic variation. This allows us to use a variable mesh size. We use the micro-price
    // to calculate the quadratic variation but store the mid-price.
    // Note that by taking log-returns of ticks, we're actually measuring the variance of ticks. In particular,
    //     var = sum_{i = 1}^N lg(r_i / r_{i-1}) = integrated variance from t_0 to t_N of ticks.
    // The termination condition for a bar is straightforward in this scheme, since we merely need to check whether
    //     var >= (1%)^2 * $BarLength / 86400
    // which is the variance a reference asset with 1% daily volatility would realise in $BarLength seconds.
    //
    // This class does not attempt to solve the problems of microstructure (though micro-price is used to avoid the bid-ask bounce)
    // and is probably not optimal given the naive ignorance towards irregular sampling. Still, it's straightforward and of
    // minimal-but-sufficient complexity. There are references at the bottom of this file about irregular sampling.
    //
    // NOTE - (micro-price + spread) is a lossy description compared to (midprice + spread) since the spread is not symmetric around
    // the micro-price. However, since I also store the LastMarket, this is not an issue for trading.
    public class VarBars : IBarList
    {
        IMessageBus Ether;

        int ContractId;
        int NumBarsToStore;

        // This is the variance that a reference asset with 1% vol would realise in the budget day-fraction.
        public double VarBudget;

        public EventHandler<Bar> BarFinalisedListeners { get; set; }

        public CircularBuffer<Bar> Bars;

        private Bar currentBar;
        private double previousLogMicro;
        private double quadraticVariation;


        public VarBars(IMessageBus ether, int contractId, int numBarsToStore, int BarLength)
        {
            Ether = ether;
            ContractId = contractId;
            NumBarsToStore = numBarsToStore;

            VarBudget = 0.0001 * BarLength / 86400.0;

            Bars = new CircularBuffer<Bar>(NumBarsToStore);

            Ether.AsObservable<Market>().Where(x => x.ContractId == ContractId).Subscribe(x =>
            {
                UpdateBar(x);
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
                throw e;
            });

            // Subscribe to end-of-day heartbeats.
            Ether.AsObservable<Heartbeat>().Where(x => x.IsDaily()).Subscribe(x =>
            {
                CompleteBar();
            },
            e =>
            {
                Debug.WriteLine(e.ToString());
                throw e;
            });

            currentBar = null;
            quadraticVariation = 0;
        }


        // We update the current Bar on every tick.
        private void UpdateBar(Market x)
        {
            if (x.Mid == decimal.MinValue || x.Mid == -1)
                return;

            if (currentBar == null)
            {
                currentBar = new Bar(x.Timestamp, x);
                quadraticVariation = 0;
                previousLogMicro = Math.Log((double)x.Micro);
            }
            else
            {
                double logPrice = Math.Log((double)x.Micro);
                double logReturn = logPrice - previousLogMicro;
                quadraticVariation += logReturn * logReturn;

                currentBar.Update(x);

                if (quadraticVariation >= VarBudget)
                    CompleteBar();

                previousLogMicro = logPrice;
            }
        }


        private void CompleteBar()
        {
            if (currentBar != null)
            {
                BarFinalisedListeners(this, currentBar);
                Bars.Insert(currentBar);

                currentBar = null;
            }
        }


        public int Length
        {
            get { return Bars.Length; }
        }


        public Bar this[int i]
        {
            get { return Bars[i]; }
        }


        public virtual IEnumerator<Bar> GetEnumerator()
        {
            return Bars.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

// NOTES:
// 9/10/12 - For trend-following strategies, I'm having trouble getting var bars to work. This might just be an issue of making
// bars a little more regular (taking EOD and weekend properly into account, for example) or it might be a more general problem
// with trying to estimate the trend when the clock is stochastic.
// 15/10/12 - First crack at fixing the irregularity problems mentioned above.



// TODO(research) - interesting: http://en.wikipedia.org/wiki/Unevenly_spaced_time_series
// interesting: http://www.eckner.com/papers/unevenly_spaced_time_series_analysis.pdf
// non-uniformly sampled variance estimator: http://projecteuclid.org/DPubS/Repository/1.0/Disseminate?view=body&id=pdf_1&handle=euclid.bj/1116340299
// Volatility & covariance estimation when microstructure noise and time are endogenous: http://www.qass.org.uk/2009-June_QASS-conference/Rosenbaum.pdf