using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ExcelDna.Integration;
using CommonTypes;


namespace XL
{
    partial class XL
    {
        [ExcelFunction(Category = "ZeusXL", Description = "Create a contract")]
        public static object CreateContract(string Symbol, string Type, string Exchange, string Currency, string LotSizeOpt, string PrimaryExchangeOpt,
                                            string MultiplierOpt, string ExpiryOpt, string RightOpt, string StrikeOpt, string SecIdTypeOpt, string SecIdOpt, string TradingClassOpt)
        {
            decimal strike = (StrikeOpt == "" ? 0 : decimal.Parse(StrikeOpt));
            int multiplier = (MultiplierOpt== "" ? 0 : int.Parse(MultiplierOpt));
            int lotSize = (LotSizeOpt == "" ? 1 : int.Parse(LotSizeOpt));
            if (Math.Sign(lotSize) != 1)
                throw new Exception(string.Format("Error, contract lot size must be positive! ({0})", lotSize));

            var contracts = XLOM.GetAll<Contract>();
            int contractId = contracts.Count() + 1;

            Contract c = new Contract(contractId, Symbol, Type, Exchange, Currency,
                                      PrimaryExchangeOpt, multiplier, ExpiryOpt, strike,
                                      RightOpt, SecIdTypeOpt, SecIdOpt, TradingClassOpt, (PositiveInteger)lotSize);

            Contract cc = contracts.Where(cs => cs.Value.Equals(c)).SingleOrDefault().Value;
            if (cc != null)
            {
                return XLOM.Key(cc);
            }
            else
            {
                return XLOM.Add(string.Format("{0}({1})", Symbol, contractId), c);
            }
        }


        [ExcelFunction(Category = "ZeusXL", Description = "Get contractId from a contract handle")]
        public static object GetContractId(string ContractHandle)
        {
            Contract cc = XLOM.Get<Contract>(ContractHandle);
            return cc.Id;
        }
    }
}
