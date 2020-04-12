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


namespace CommonTypes
{
    [DataContract]
    public class Contract : IB.Contract, IIdentifiable {
        public int Id { get; private set; }

        public string Type
        {
            get
            {
                return SecType;
            }
            set
            {
                SecType = value;
            }
        }

        new public int Multiplier
        {
            get
            {
                return int.Parse(base.Multiplier);
            }
        }

        public Contract(int id, string symbol, string type, string exchange, string currency, string primaryExchange,
                        string multiplier, string expiry, decimal strike, string right, string secIdType, string secId, string tradingClass, PositiveInteger lotSize)
        {
            Id = id;
            Symbol = symbol;
            Type = type;
            Exchange = exchange;
            PrimaryExch = primaryExchange;
            Currency = currency;
            base.Multiplier = multiplier;
            Expiry = expiry;

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
            PrimaryExch = c.PrimaryExch;
            Currency = c.Currency;
            base.Multiplier = c.Multiplier.ToString();
            Expiry = c.Expiry;
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
            PrimaryExch = c.PrimaryExch;
            Currency = c.Currency;
            base.Multiplier = c.Multiplier.ToString();
            Expiry = c.Expiry;
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

            IBApi.Contract other = (IBApi.Contract)p_other;

            if (other.Symbol != Symbol ||
                other.Exchange != Exchange ||
                other.Currency != Currency ||
                other.Multiplier != base.Multiplier ||
                other.SecType != SecType ||
                other.Expiry != Expiry ||
                other.Right != Right ||
                other.Strike != Strike ||
                other.TradingClass != TradingClass)
                return false;

            return true;
        }
    }
}