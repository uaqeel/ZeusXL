using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;


namespace CommonTypes
{
    [DataContract]
    public struct PositiveInteger
    {
        [DataMember]
        public int Value { get; private set; }

        public PositiveInteger(int vv) : this()
        {
            Value = Math.Abs(vv);
        }

        public PositiveInteger(double vv)
            : this()
        {
            Value = (int)Math.Round(Math.Abs(vv));
        }

        public PositiveInteger(decimal vv)
            : this()
        {
            Value = (int)Math.Round(Math.Abs(vv));
        }

        // Don't make this implicit.
        public static explicit operator PositiveInteger(int vv)
        {
            return new PositiveInteger(vv);
        }

        // Don't make this implicit.
        public static explicit operator PositiveInteger(double vv)
        {
            return new PositiveInteger(vv);
        }

        public static PositiveInteger operator *(PositiveInteger p1, PositiveInteger p2)
        {
            return new PositiveInteger(p1.Value * p2.Value);
        }


        public static PositiveInteger operator *(PositiveInteger p1, int i1)
        {
            return new PositiveInteger(p1.Value * i1);
        }


        public static PositiveInteger operator +(PositiveInteger p1, PositiveInteger p2)
        {
            return new PositiveInteger(p1.Value + p2.Value);
        }
    }


    public struct DataSourceInfo
    {
        public string DataSourceType;
        public int ContractId;
        public string Directory;

        public bool InMemory;

        public DataSourceInfo(string t, int c, string d)
        {
            DataSourceType = t;
            ContractId = c;
            Directory = d;

            InMemory = false;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", DataSourceType, ContractId, Directory);
        }

        public override bool Equals(object rhs)
        {
            if (rhs.GetType() != typeof(DataSourceInfo))
                return false;

            DataSourceInfo drhs = (DataSourceInfo)rhs;
            return (drhs.DataSourceType == DataSourceType && drhs.ContractId == ContractId && drhs.Directory == Directory);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


    public class PositionTuple
    {
        public int Size;
        public decimal AverageCost;
        public decimal UnrealisedPnL;

        public PositionTuple(int size, decimal avgCost, decimal unrealisedPnL)
        {
            Size = size;
            AverageCost = avgCost;
            UnrealisedPnL = unrealisedPnL;
        }
    }
}
