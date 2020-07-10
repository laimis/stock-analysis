using System;
using System.Collections.Generic;
using System.Linq;
using core.Portfolio.Output;

namespace core.Options
{
    public class OwnedOptionSummary
    {
        public OwnedOptionSummary(){}
        public OwnedOptionSummary(OwnedOption o, TickerPrice currentPrice)
        {
            Id = o.State.Id;
            Ticker = o.State.Ticker;
            CurrentPrice = currentPrice.Amount;
            OptionType = o.State.OptionType.ToString();
            StrikePrice = o.State.StrikePrice;
            PremiumReceived = o.State.PremiumReceived;
            PremiumPaid = o.State.PremiumPaid;
            ExpirationDate = o.State.Expiration.ToString("yyyy-MM-dd");
            NumberOfContracts = Math.Abs(o.State.NumberOfContracts);
            BoughtOrSold = o.State.SoldToOpen.Value ? "Sold" : "Bought";
            Filled = o.State.FirstFill;
            Days = o.State.Days;
            DaysHeld = o.State.DaysHeld;
            Transactions = new TransactionList(o.State.Transactions.Where(t => !t.IsPL), null, null);
            ExpiresSoon = o.ExpiresSoon;
            IsExpired = o.IsExpired;
            Closed = o.Closed;
            Assigned = o.State.Assigned;

            if (!currentPrice.NotFound)
            {
                ItmOtmLabel = GetItmOtmLabel(o, currentPrice.Amount);
                IsFavorable = GetIsFavorable();
                StrikePriceDiff = Math.Abs(o.State.StrikePrice - currentPrice.Amount)/currentPrice.Amount;
            }
        }

        private bool GetIsFavorable()
        {
            return _favorableLogic[BoughtOrSold](ItmOtmLabel);
        }

        private static Dictionary<string, Func<string, bool>> _favorableLogic = new Dictionary<string, Func<string, bool>> {
            { "Bought", (otm) => otm != "OTM"},
            { "Sold", (otm) => otm == "OTM"}
        };

        private static Dictionary<string, Func<double, double, string>> _otmLogic = new Dictionary<string, Func<double, double, string>> {
            { core.Options.OptionType.CALL.ToString(), (price, strike) => {
                if (price > strike) return "ITM";
                else if (price == strike) return "ATM";
                else return "OTM";
            }},
            { core.Options.OptionType.PUT.ToString(), (price, strike) => {
                if (price > strike) return "OTM";
                else if (price == strike) return "ATM";
                else return "ITM";
            }},
        };
        private string GetItmOtmLabel(OwnedOption o, double amount)
        {
            return _otmLogic[OptionType](amount, o.State.StrikePrice);
        }

        public Guid Id { get; set; }
        public string Ticker { get; set; }
        public double CurrentPrice { get; }
        public string OptionType { get; set; }
        public double StrikePrice { get; set; }
        public double PremiumReceived { get; set; }
        public double PremiumPaid { get; set; }
        public double PremiumCapture
        {
            get
            {
                if (this.BoughtOrSold == "Bought")
                {
                    return (PremiumReceived - PremiumPaid) / PremiumPaid;
                }

                return (PremiumReceived - PremiumPaid) / PremiumReceived;
            }
        }
        public double Profit => this.PremiumReceived - this.PremiumPaid;
        public string ExpirationDate { get; set; }
        public int NumberOfContracts { get; set; }
        public string BoughtOrSold { get; set; }
        public DateTimeOffset Filled { get; set; }
        public double Days { get; set; }
        public int DaysHeld { get; set; }
        public TransactionList Transactions { get; set; }
        public bool ExpiresSoon { get; set; }
        public bool IsExpired { get; set; }
        public DateTimeOffset? Closed { get; }
        public bool Assigned { get; set; }
        public string ItmOtmLabel { get; }
        public bool IsFavorable { get; }
        public double StrikePriceDiff { get; }
    }
}