using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Common.Types
{
    public enum TimePeriods : int
    {
        OneSecond = 1,
        FiveSeconds = 5,
        FifteenSeconds = 15,
        ThirtySeconds = 30,
        OneMinute = 60,
        TwoMinutes = 120,
        FiveMinutes = 300,
        FifteenMinutes = 900,
        ThirtyMinutes = 1800,
        OneHour = 3600,
        SixHours = 21600,
        TwelveHours = 43200,
        OneDay = 86400,
        OneWeek = 604800,
        OneMonth = 2629800,                 // 86400 * 365.25 / 12
        OneYear = 31557600,
        OneBusinessYear = 86400 * 252
    }


    public static class TimeUtils
    {
        //
        // Summary:
        //     Returns a new System.DateTime that adds the specified number of seconds, milliseconds and
        //     microseconds to the value of this instance.
        //
        // Parameters:
        //   value:
        //     A number of seconds, positive or negative.
        //
        // Returns:
        //     A System.DateTime whose value is the sum of the date and time represented
        //     by this instance and the time represented by value.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     The resulting System.DateTime is less than System.DateTime.MinValue or greater
        //     than System.DateTime.MaxValue.
        public static DateTime AddSecondsPrecisely(this DateTime dt, double secs)
        {
            double nTicks = secs * 1e7;
            return dt.AddTicks((long)nTicks);
        }
    }
}
