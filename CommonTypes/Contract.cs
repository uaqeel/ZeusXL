using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using IB = IBApi;
using Accord;

namespace CommonTypes
{
    [DataContract]
    public class Contract : IIdentifiable {
        public IB.Contract IBContract { get; private set; }
        public int Id { get; private set; }

        public string Type
        {
            get
            {
                return IBContract.SecType;
            }
            set
            {
                IBContract.SecType = value;
            }
        }

        public string Symbol
        {
            get { return IBContract.Symbol; }
            set { IBContract.Symbol = value; }
        }

        public string Exchange
        {
            get { return IBContract.Exchange; }
            set { IBContract.Exchange = value; }
        }

        public string PrimaryExchange
        {
            get { return IBContract.PrimaryExch; }
            set { IBContract.PrimaryExch = value; }
        }

        public string Currency
        {
            get { return IBContract.Currency; }
            set { IBContract.Currency = value; }
        }

        private int multiplier;
        public int Multiplier
        {
            get { return multiplier; }
            set { multiplier = value;
                  IBContract.Multiplier = (multiplier == 0 ? "" : multiplier.ToString());
            }
        }

        public string LastTradeDateOrContractMonth
        {
            get { return IBContract.LastTradeDateOrContractMonth; }
            set { IBContract.LastTradeDateOrContractMonth = value; }
        }

        public double Strike
        {
            get { return IBContract.Strike; }
            set { IBContract.Strike = value; }
        }

        public string Right
        {
            get { return IBContract.Right; }
            set { IBContract.Right = value; }
        }

        public string SecId
        {
            get { return IBContract.SecId; }
            set { IBContract.SecId = value; }
        }

        public string SecIdType
        {
            get { return IBContract.SecIdType; }
            set { IBContract.SecIdType = value; }
        }

        public string TradingClass
        {
            get { return IBContract.TradingClass; }
            set { IBContract.TradingClass = value; }
        }

        public Contract(int id, string symbol, string type, string exchange, string currency, string primaryExchange,
                        int multiplier, string expiry, decimal strike, string right, string secIdType, string secId, string tradingClass, PositiveInteger lotSize)
        {
            IBContract = new IB.Contract();

            Id = id;
            Symbol = symbol;
            Type = type;
            Exchange = exchange;
            PrimaryExchange = primaryExchange;
            Currency = currency;
            Multiplier = multiplier;
            LastTradeDateOrContractMonth = expiry;

            Strike = (double)strike;
            Right = right;

            SecIdType = secIdType;
            SecId = secId;

            TradingClass = tradingClass;
        }


        public Contract(Contract c)
        {
            Id = c.Id;
            Symbol = c.Symbol;
            Type = c.Type;
            Exchange = c.Exchange;
            PrimaryExchange = c.PrimaryExchange;
            Currency = c.Currency;
            Multiplier = c.Multiplier;
            LastTradeDateOrContractMonth = c.LastTradeDateOrContractMonth;
            Strike = c.Strike;
            Right = c.Right;

            SecIdType = c.SecIdType;
            SecId = c.SecId;
        }


        public Contract(string xml, int id)
        {
            Type[] serialisableTypes = (from t in Assembly.GetExecutingAssembly().GetTypes()
                                        from a in t.GetCustomAttributes(false)
                                        where a.GetType().Equals(typeof(System.Runtime.Serialization.DataContractAttribute))
                                        select t).ToArray();

            DataContractSerializer ser = new DataContractSerializer(typeof(Contract), serialisableTypes);
            Contract c = ser.ReadObject(new MemoryStream(Encoding.ASCII.GetBytes(xml))) as Contract;
            Id = id;
            Symbol = c.Symbol;
            Type = c.Type;
            Exchange = c.Exchange;
            PrimaryExchange = c.PrimaryExchange;
            Currency = c.Currency;
            Multiplier = c.Multiplier;
            LastTradeDateOrContractMonth = c.LastTradeDateOrContractMonth;
            Strike = c.Strike;
            Right = c.Right;
            SecIdType = c.SecIdType;
            SecId = c.SecId;
            TradingClass = c.TradingClass;
        }


        public override string ToString()
        {
            Type[] serialisableTypes = (from t in Assembly.GetExecutingAssembly().GetTypes()
                                        from a in t.GetCustomAttributes(false)
                                        where a.GetType().Equals(typeof(System.Runtime.Serialization.DataContractAttribute))
                                        select t).ToArray();

            DataContractSerializer ser = new DataContractSerializer(typeof(Contract), serialisableTypes);
            MemoryStream ms = new MemoryStream();

            try
            {
                ser.WriteObject(ms, this);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }

            string serialized = System.Text.ASCIIEncoding.ASCII.GetString(ms.ToArray());
            return serialized;
        }


        public override bool Equals(Object p_other)
        {
            if (p_other == this)
                return true;
            else if (p_other == null)
                return false;

            Contract other = (Contract)p_other;

            if (other.Symbol != Symbol ||
                other.Exchange != Exchange ||
                other.Currency != Currency ||
                other.Multiplier != Multiplier ||
                other.Type != Type ||
                other.LastTradeDateOrContractMonth != LastTradeDateOrContractMonth ||
                other.Right != Right ||
                other.Strike != Strike ||
                other.TradingClass != TradingClass)
                return false;

            return true;
        }
    }
}