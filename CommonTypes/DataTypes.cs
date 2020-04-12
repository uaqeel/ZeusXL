using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonTypes
{
    public interface IIdentifiable
    {
        int Id { get; }
    }


    public interface ITimestampedDatum
    {
        DateTimeOffset Timestamp { get; }
    }


    public struct Heartbeat : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }
        public int Interval;
        public string Message;

        public Heartbeat(DateTimeOffset timestamp, int interval, string message) : this()
        {
            Timestamp = timestamp;
            Interval = interval;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("{0} @ {1} ({2})", Message, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), Interval);
        }


        public bool Is5Minutely()
        {
            return Timestamp.TimeOfDay.TotalSeconds % 300 == 0;
        }


        public bool IsHourly()
        {
            return Timestamp.TimeOfDay.TotalSeconds % 3600 == 0;
        }


        public bool IsDaily()
        {
            return Timestamp.TimeOfDay == TimeSpan.Zero;
        }
    }


    public struct PnLInfo : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; private set; }

        public int StrategyId { get; private set; }
        public decimal CumulativePnL { get; private set; }
        public decimal CurrentAccountValue { get; private set; }                            // This is per-strategy.
        public decimal OverallCurrentAccountValue { get; private set; }                     // This is NOT per-strategy.

        public PnLInfo(DateTimeOffset timestamp, int strategyId, decimal cumPnl, decimal currAccountSize, decimal overallCurrAccountSize) : this()
        {
            Timestamp = timestamp;
            StrategyId = strategyId;
            CumulativePnL = cumPnl;
            CurrentAccountValue = currAccountSize;
            OverallCurrentAccountValue = overallCurrAccountSize;
        }


        public override string ToString()
        {
            return string.Format("{0} @ {1}: {2} (Account Size: {3}, {4})",
                                 StrategyId, Timestamp.ToString("yyyy-MM-dd HH:mm:ss.ffffff"), CumulativePnL, CurrentAccountValue, OverallCurrentAccountValue);
        }
    }


    public struct MarketHours : ITimestampedDatum
    {
        public int ContractId;
        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset LiquidTradingStart;
        public DateTimeOffset LiquidTradingEnd;
        public DateTimeOffset MarketTradingStart;
        public DateTimeOffset MarketTradingEnd;


        public MarketHours(int cId, DateTimeOffset ts, DateTimeOffset ls, DateTimeOffset le, DateTimeOffset ms, DateTimeOffset me)
            : this()
        {
            ContractId = cId;
            Timestamp = Timestamp;

            LiquidTradingStart = ls;
            LiquidTradingEnd = le;

            MarketTradingStart = ms;
            MarketTradingEnd = me;
        }


        // This is meant to be temporary...
        public MarketHours(DateTimeOffset today, TimeSpan open, TimeSpan close)
            : this()
        {
            ContractId = -1;
            Timestamp = today;

            MarketTradingStart = LiquidTradingStart = today.Date + open;
            MarketTradingEnd = LiquidTradingEnd = today.Date + close;
        }


        public bool IsCurrentlyLiquid(DateTimeOffset now)
        {
            if (now > LiquidTradingStart && now < LiquidTradingEnd)
                return true;

            return false;
        }


        public bool IsCurrentlyTrading(DateTimeOffset now)
        {
            if (now > MarketTradingStart && now < MarketTradingEnd)
                return true;

            return false;
        }
    }


    public struct AccountInfo : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }
        public string AccountName;
        public string BaseCurrency;
        public decimal NetLiquidationValue;
        public decimal InitialMarginRequirement;
        public decimal MaintenanceMarginRequirement;
        public decimal GrossPositionValue;
        public decimal Leverage;


        public AccountInfo(string accountName) : this()
        {
            Timestamp = DateTimeOffset.MinValue;

            AccountName = accountName;
            BaseCurrency = string.Empty;
            NetLiquidationValue = -1;
            InitialMarginRequirement = -1;
            MaintenanceMarginRequirement = -1;
            GrossPositionValue = -1;
            Leverage = -1;
        }
        

        public bool IsPrimed
        {
            get
            {
                if (BaseCurrency != string.Empty && NetLiquidationValue != -1 && InitialMarginRequirement != -1
                    && MaintenanceMarginRequirement != -1 && GrossPositionValue != -1 && Leverage != -1)
                    return true;

                return false;
            }
        }
    }


    public struct DepositRate : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Currency;
        public double Value { get; private set; }

        public DepositRate(DateTimeOffset timestamp, string currency, double value)
            : this()
        {
            Timestamp = timestamp;
            Currency = currency;
            Value = value;
        }
    }


    public struct ImpliedVol : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }
        public int ContractId;
        public double Value { get; private set; }

        public ImpliedVol(DateTimeOffset timestamp, int contractId, double value)
            : this()
        {
            Timestamp = timestamp;
            ContractId = contractId;
            Value = value;
        }
    }


    public struct Price : ITimestampedDatum
    {
        public DateTimeOffset Timestamp { get; set; }
        public double Value { get; private set; }

        public Price(DateTimeOffset timestamp, double value)
            : this()
        {
            Timestamp = timestamp;
            Value = value;
        }


        public Market ToMarket()
        {
            return new Market(Timestamp, -1, (decimal)Value, (decimal)Value);
        }
    }


    public struct StrategyInfo
    {
        public int StrategyId;

        public StrategyInfo(int s)
        {
            StrategyId = s;
        }
    }
}
