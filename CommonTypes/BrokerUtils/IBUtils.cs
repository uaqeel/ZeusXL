using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IB = IBApi;

namespace CommonTypes.BrokerUtils
{
    public static class IBUtils
    {
        public static DateTime DateFromIB(this string d)
        {
            string[] tokens = d.Split(' ');

            DateTime dt;
            if (tokens.Length > 1)
            {
                int yyyy = int.Parse(tokens[0].Substring(0, 4));
                int mm = int.Parse(tokens[0].Substring(4, 2));
                int dd = int.Parse(tokens[0].Substring(6, 2));

                TimeSpan ts = TimeSpan.Parse(tokens[2]);

                dt = new DateTime(yyyy, mm, dd, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            }
            else
            {
                dt = DateTime.Parse(tokens[0]);
            }

            return dt;
        }
    }


    public enum IBTickType : int
    {
        BidSize = 0,
        BidPrice,

        MidPrice,

        AskPrice,
        AskSize,

        OpenPrice,
        ClosePrice,
        HighPrice,
        LowPrice,

        LastPrice,
        LastSize,
        Volume,

        Spread,

        ImpliedVol,
        Delta,
        Gamma,
        Theta,

        NumTickTypes
    }
}
