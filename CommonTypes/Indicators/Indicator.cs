using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using CommonTypes;


// Indicators are just data transformations. Note that I took out the Initialise()
// function since we no longer want Indicators to store state.

// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators
namespace CommonTypes
{
    public abstract class Indicator
    {
        public abstract double Update(double newValue);
        public abstract double Value { get; }


        // Base class implementation for those indicators that don't need highs/lows, etc.
        public virtual double Update(Bar completeBar)
        {
            return Update((double)completeBar.Close);
        }


        public static Indicator Create(string type, Dictionary<string, object> config)
        {
            try
            {
                Type t = CommonTypes.Utils.FindType(type, "Strategies");
                return Activator.CreateInstance(t, new object[] { config }) as Indicator;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }
    }
}
