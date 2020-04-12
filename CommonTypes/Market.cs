using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ProtoBuf;


// TODO(1) - http://stackoverflow.com/questions/9107010/variable-precision-float-double-values
namespace CommonTypes
{
    [ProtoContract]
    public class Market : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }

        public int ContractId { get; set; }

        [ProtoMember(1)]
        private long _BidSize;

        [ProtoMember(2)]
        private decimal _Bid;

        [ProtoMember(3)]
        private decimal _Ask;

        [ProtoMember(4)]
        private long _AskSize;

        [ProtoMember(5)]
        public OptionData OptionData;

        [ProtoMember(6)]
        public decimal PreviousClose;


        // The decimal.MinValue checks make this less efficient, but never mind for now...
        // NOTE: Changing the default value of Mid will break RTD.
        private decimal _Mid = decimal.MinValue, _Micro = decimal.MinValue;


        // Workaround for protobuf limitation.
        [ProtoMember(5)]
        public string TimestampString
        {
            get { return Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"); }
            set { Timestamp = DateTimeOffset.Parse(value); }
        }


        public Market()
        {
            // For protobuf.
        }


        public Market(DateTimeOffset timestamp, int contractId, int bidsize, decimal bid, decimal ask, int asksize)
        {
            Timestamp = timestamp;
            ContractId = contractId;
            BidSize = bidsize;
            Bid = bid;
            Ask = ask;
            AskSize = asksize;
        }


        public Market(DateTimeOffset timestamp, int contractId, decimal bid, decimal ask)
            : this(timestamp, contractId, int.MinValue, bid, ask, int.MinValue)
        {
        }


        public Market(Market m)
        {
            Timestamp = m.Timestamp;
            ContractId = m.ContractId;
            BidSize = m.BidSize;
            Bid = m.Bid;
            Ask = m.Ask;
            AskSize = m.AskSize;
        }


        public long BidSize
        {
            get { return _BidSize; }
            set
            {
                _BidSize = value;
                _Micro = decimal.MinValue;
            }
        }


        public decimal Bid
        {
            get { return _Bid; }
            set
            {
                _Bid = value;
                _Mid = _Micro = decimal.MinValue;
            }
        }


        public decimal Ask
        {
            get { return _Ask; }
            set
            {
                _Ask = value;
                _Mid = _Micro = decimal.MinValue;
            }
        }


        public long AskSize
        {
            get { return _AskSize; }
            set
            {
                _AskSize = value;
                _Micro = decimal.MinValue;
            }
        }


        public decimal Mid
        {
            get
            {
                if (_Mid == decimal.MinValue) {
                    if (Ask != decimal.MinValue && Bid != decimal.MinValue)
                        _Mid = (decimal)0.5 * (Ask + Bid);
                    else if (Ask != decimal.MinValue)
                        _Mid = Ask;
                    else if (Bid != decimal.MinValue)
                        _Mid = Bid;
                }

                return _Mid;
            }
        }


        // The micro-price counters bid-ask bounce effects and does seem to have
        // more predictive power than the mid-price.
        //
        // Micro = (v_b * p_a + v_a * p_b) / (v_a + v_b); Mid = (p_b + p_a) / 2.
        // Micro - Mid = 0.5 * (v_a - v_b) * (p_b - p_a) / (v_a + v_b)
        //             = 0.5 * (v_b - v_a) / (v_a + v_b) * (p_a - p_b)
        //             = 0.5 * (% volume imbalance) * (spread).
        public decimal Micro
        {
            get
            {
                if (_Micro == decimal.MinValue)
                {
                    if (AskSize == int.MinValue || BidSize == int.MinValue || AskSize + BidSize == 0)
                        _Micro = Mid;
                    else
                    {
                        try
                        {
                            _Micro = (decimal)(Bid * AskSize + Ask * BidSize) / (AskSize + BidSize);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(this);
                            Debug.WriteLine(e.ToString());
                        }
                    }
                }

                return _Micro;
            }
        }


        public decimal Spread
        {
            get
            {
                if (Ask != decimal.MinValue && Bid != decimal.MinValue)
                    return Ask - Bid;
                else
                    return 0;
            }
        }


        public override string ToString()
        {
            return string.Format("({0} @ {2}: {3},{4} - {5},{6})", ContractId, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
                                                                   BidSize, Bid, Ask, AskSize);
        }


        public bool Equals(Market rhs)
        {
            if (Timestamp != rhs.Timestamp ||
                ContractId != rhs.ContractId ||
                BidSize != rhs.BidSize ||
                Bid != rhs.Bid ||
                Ask != rhs.Ask ||
                AskSize != rhs.AskSize)
                return false;

            return true;
        }
    }


    [ProtoContract]
    public class OptionData
    {
        [ProtoMember(7)]
        public decimal RefPrice;

        [ProtoMember(8)]
        public decimal OptionPrice;

        [ProtoMember(9)]
        public double ImpliedVol;

        [ProtoMember(10)]
        public double Delta;

        [ProtoMember(11)]
        public double Gamma;

        [ProtoMember(12)]
        public double Theta;

        public OptionData(decimal refPrice, decimal optionPrice, double impliedVol, double delta, double gamma, double theta)
        {
            RefPrice = refPrice;
            OptionPrice = optionPrice;
            ImpliedVol = impliedVol;
            Delta = delta;
            Gamma = gamma;
            Theta = theta;
        }
    }
}
