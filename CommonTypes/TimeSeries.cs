using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

using ProtoBuf;

using Common;
using Common.Types;


namespace Mnemosyne
{
    // These are single-underlying.
    [ProtoContract]
    public class TimeSeries : IEnumerable<Market>
    {
        [ProtoMember(1)]
        public int NumRecords;
        [ProtoMember(2)]
        public Market[] Markets;

        private static char[] delimiters = new char[] { ',', ';' };


        public TimeSeries()
        {
        }


        public TimeSeries(int numRecords)
        {
            NumRecords = numRecords;
            Markets = new Market[NumRecords];
        }


        public static TimeSeries Parse(int ContractId, string[] Records)
        {
            return Parse(ContractId, Records, DateTime.MinValue);
        }


        // StartDayOptional is used to 
        //  a) start a time series at a particular day in the past
        //  b) enforce a particular ordering of an array of TimeSeries.
        // For example, suppose we have the following:
        //
        //      C:\Workspace\s\Zeus\Sheets\MD\ALEX_EURUSD\CASH_EUR__IDEALPRO_USD_20091204.log
        //      C:\Workspace\s\Zeus\Sheets\MD\ALEX_EURUSD\CASH_EUR__IDEALPRO_USD_20091206.log
        //      C:\Workspace\s\Zeus\Sheets\MD\ALEX_EURUSD\CASH_EUR__IDEALPRO_USD_20091129.log
        //      C:\Workspace\s\Zeus\Sheets\MD\ALEX_EURUSD\CASH_EUR__IDEALPRO_USD_20091207.log
        //      C:\Workspace\s\Zeus\Sheets\MD\ALEX_EURUSD\CASH_EUR__IDEALPRO_USD_20091208.log
        //
        // Now we can parse these with start days (20090101, 20090102, ...), to ensure that when we
        // stitch them together, the stitching works properly.
        // Note that the primary use is for Alex's data format -- stitching often doesn't work because
        // all the time series are loaded with date == today's date (since the date isn't included
        // in the file). Also, since the date isn't included, we tend to go forward in time from today's
        // date rather than keeping our historical perspective.
        // TODO: I don't use StartDayOptional for the other sources because it isn't necessary there -- reconsider.
        public static TimeSeries Parse(int ContractId, string[] Records, DateTime StartDayOptional)
        {
            // Keep the IB date, GAIN date and Alex date as the last three...
            string[] timeHeaders = new string[] { "TIME", "TIMESTAMP", "DATE", "ZEIT", "RATEDATETIME", "DATE/TIME" };
            string[] bidHeaders = new string[] { "BID", "BIDPRICE", "RATEBID" };
            string[] bSizeHeaders = new string[] { "BIDSIZE", "BSIZE" };

            string[] askHeaders = new string[] { "ASK", "ASKPRICE", "RATEASK" };
            string[] aSizeHeaders = new string[] { "ASKSIZE", "ASIZE" };

            string[] barHeaders = new string[] { "OPEN", "OPENPRICE", "CLOSE", "CLOSEPRICE", "HIGH", "HIGHPRICE", "LOW", "LOWPRICE" };

            int dateIndex = -1, bidIndex = -1, askIndex = -1;
            int bSizeIndex = -1, aSizeIndex = -1;
            int openIndex = -1, closeIndex = -1, highIndex = -1, lowIndex = -1;
            bool isIBDate = false, isGAINDate = false, isAlexDate = false;

            string[] headers = Records[0].Split(delimiters);
            for (int i = 0; i < headers.Length; ++i)
            {
                string h = headers[i].ToUpper();
                if (timeHeaders.Contains(h))
                {
                    dateIndex = i;

                    if (h == timeHeaders[timeHeaders.Length - 3])
                    {
                        isAlexDate = true;
                        if (StartDayOptional == DateTime.MinValue)
                            StartDayOptional = DateTime.Now.Date;
                    }
                    else if (h == timeHeaders[timeHeaders.Length - 2])
                        isGAINDate = true;
                    else if (h == timeHeaders.Last())
                        isIBDate = true;
                }
                else if (bidHeaders.Contains(h))
                {
                    bidIndex = i;
                }
                else if (bSizeHeaders.Contains(h))
                {
                    bSizeIndex = i;
                }
                else if (askHeaders.Contains(h))
                {
                    askIndex = i;
                }
                else if (aSizeHeaders.Contains(h))
                {
                    aSizeIndex = i;
                }
                else if (barHeaders.Contains(h))
                {
                    if (h.StartsWith("OPEN"))
                    {
                        openIndex = i;
                    }
                    else if (h.StartsWith("CLOSE"))
                    {
                        closeIndex = i;
                    }
                    else if (h.StartsWith("HIGH"))
                    {
                        highIndex = i;
                    }
                    else if (h.StartsWith("LOW"))
                    {
                        lowIndex = i;
                    }
                }
            }


            // This is used to ensure that we only use the final "bar" for each time. Gets around issues in Alex's
            // data, for example.
            Dictionary<DateTime, Market> temp = new Dictionary<DateTime,Market>();
            for (int i = 0; i < Records.Length - 1; ++i)
            {
                string s = Records[i + 1];
                string[] tokens = s.Split(delimiters);

                DateTime now;
                if (isIBDate)
                {
                    int year = Int32.Parse(tokens[0].Substring(0, 4));
                    int month = Int32.Parse(tokens[0].Substring(4, 2));
                    int day = Int32.Parse(tokens[0].Substring(6, 2));

                    DateTime time = DateTime.Parse(tokens[0].Substring(10));
                    now = new DateTime(year, month, day, time.Hour, time.Minute, time.Second, time.Millisecond);
                }
                else if (isGAINDate)
                {
                    now = DateTime.Parse(tokens[dateIndex]);
                    now = now.AddMilliseconds(i % 1000);
                }
                else if (isAlexDate)
                {
                    string time = TimeSeries.ReplaceLastOccurrence(tokens[dateIndex], ":", ".");
                    now = DateTime.Parse(time);

                    now = StartDayOptional.Date.Add(now - now.Date);
                }
                else
                {
                    now = DateTime.Parse(tokens[dateIndex]);
                }

                if (!temp.ContainsKey(now))
                    temp[now] = new Market(ContractId);

                Market m = temp[now];
                if (bidIndex != -1)
                    m[TickType.BidPrice, now] = Double.Parse(tokens[bidIndex]);

                if (bSizeIndex != -1)
                    m[TickType.BidSize, now] = Double.Parse(tokens[bSizeIndex]);

                if (askIndex != -1)
                    m[TickType.AskPrice, now] = Double.Parse(tokens[askIndex]);

                if (aSizeIndex != -1)
                    m[TickType.AskSize, now] = Double.Parse(tokens[aSizeIndex]);

                if (openIndex != -1)
                    m[TickType.OpenPrice, now] = Double.Parse(tokens[openIndex]);

                if (closeIndex != -1)
                    m[TickType.ClosePrice, now] = Double.Parse(tokens[closeIndex]);

                if (highIndex != -1)
                    m[TickType.HighPrice, now] = Double.Parse(tokens[highIndex]);

                if (lowIndex != -1)
                    m[TickType.LowPrice, now] = Double.Parse(tokens[lowIndex]);


                if (bidIndex == -1 && askIndex == -1 && closeIndex != -1)
                    m[TickType.MidPrice, now] = m[TickType.ClosePrice];
            }

            TimeSeries t = new TimeSeries(temp.Count);
            t.Markets = temp.Values.OrderBy(x => x.Timestamp).ToArray();

            return t;
        }


        public static TimeSeries Parse(int ContractId, byte[] BinaryRecords)
        {
            MemoryStream ms = new MemoryStream(BinaryRecords);
            PriceData[] td = Serializer.Deserialize<PriceData[]>(ms);

            TimeSeries ts = new TimeSeries(td.Length);

            for (int i = 0; i < td.Length; ++i)
            {
                Market m = new Market(ContractId);

                m[TickType.BidPrice, td[i].Timestamp] = td[i].Bid;
                m[TickType.AskPrice, td[i].Timestamp] = td[i].Ask;

                ts.Markets[i] = m;
            }

            return ts;
        }


        // This function will:
        // - modify the timestamps on this time series so that they follow the last tick in the preceding time series (date + hour + minute + seconds will be replaced)
        // - scale the spots in this time series by a factor of (first spot in this series / last spot in the preceding series)
        public void StitchTo(TimeSeries precedingTimeseries)
        {
            Market lastPreceding = precedingTimeseries.Markets.Last();

            double timeGap = (Markets[0].Timestamp - lastPreceding.Timestamp).TotalSeconds;
            if (Math.Abs(timeGap) > 60)                                                                                   // 1 minute.
            {
                foreach (Market m in Markets)
                {
                    m.Timestamp = m.Timestamp.AddSecondsPrecisely(-timeGap.AsInt() + 1);

                    if (m.Timestamp <= lastPreceding.Timestamp)
                        throw new Exception("Error stitching time series together (synching)!");
                }
            }

            double scaleGap = Markets[0][TickType.MidPrice] / lastPreceding[TickType.MidPrice] - 1;
            if (Math.Abs(scaleGap) > 0.01)                                                                                // 1%
            {
                foreach (Market m in Markets)
                {
                    m[TickType.BidPrice, m.Timestamp] = Math.Round(m[TickType.BidPrice] / (1 + scaleGap), 8);
                    m[TickType.AskPrice, m.Timestamp] = Math.Round(m[TickType.AskPrice] / (1 + scaleGap), 8);

                    if (m[TickType.MidPrice].Equals(double.NaN))
                        throw new Exception("Error stitching time series together (scaling)!");
                }
            }
        }


        public IEnumerator<Market> GetEnumerator()
        {
            foreach (Market m in Markets)
                yield return m;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.LastIndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }
    }


    [ProtoContract]
    class PriceData
    {
        [ProtoMember(1)]
        public DateTime Timestamp;

        [ProtoMember(2)]
        public double Bid;

        [ProtoMember(3)]
        public double Ask;

        public PriceData()
        {
        }
    }
}
