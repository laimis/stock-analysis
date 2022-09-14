using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionView
    {
        public OwnedOptionView(){}
        public OwnedOptionView(OwnedOption o)
        {
            Id = o.State.Id;
            Ticker = o.State.Ticker;
            OptionType = o.State.OptionType.ToString();
            StrikePrice = o.State.StrikePrice;
            ExpirationDate = o.State.Expiration.ToString("yyyy-MM-dd");
            NumberOfContracts = Math.Abs(o.State.NumberOfContracts);
            BoughtOrSold = o.State.SoldToOpen.Value ? "Sold" : "Bought";
            Filled = o.State.FirstFill.Value;
            Days = o.State.Days;
            DaysHeld = o.State.DaysHeld;
            Transactions = o.State.Transactions.Where(t => !t.IsPL).ToList();
            ExpiresSoon = o.State.ExpiresSoon;
            IsExpired = o.State.IsExpired;
            Closed = o.State.Closed;
            Assigned = o.State.Assigned;
            Notes = o.State.Notes;

            var credits = o.State.Transactions.Where(t => !t.IsPL && t.Amount >= 0);
            var debits = o.State.Transactions.Where(t => !t.IsPL && t.Amount < 0);

            if (credits.Any()) PremiumReceived = credits.Sum(t => t.Amount);
            if (debits.Any()) PremiumPaid = debits.Sum(t => t.Amount);
        }

        public OwnedOptionView(OwnedOption option, decimal currentPrice)
            : this(option)
        {
            ApplyPrice(currentPrice);
        }

        public void ApplyPrice(decimal price)
        {
            CurrentPrice = price;
            ItmOtmLabel = GetItmOtmLabel(price, OptionType, StrikePrice);
            IsFavorable = GetIsFavorable();
            if (price > 0)
            {
                StrikePriceDiff = Math.Abs(StrikePrice - price) / price;
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

        private static Dictionary<string, Func<decimal, decimal, string>> _otmLogic = new Dictionary<string, Func<decimal, decimal, string>> {
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
        
        public static string GetItmOtmLabel(decimal currentPrice, string optionType, decimal strikePrice)
        {
            return _otmLogic[optionType](currentPrice, strikePrice);
        }

        public Guid Id { get; set; }
        public string Ticker { get; set; }
        public decimal CurrentPrice { get; set; }
        public string OptionType { get; set; }
        public decimal StrikePrice { get; set; }
        public decimal PremiumReceived { get; set; }
        public decimal PremiumPaid { get; set; }
        public decimal PremiumCapture
        {
            get
            {
                if (BoughtOrSold == "Bought")
                {
                    return (PremiumReceived - PremiumPaid) / PremiumPaid;
                }

                return (PremiumReceived - PremiumPaid) / PremiumReceived;
            }
        }
        public decimal Profit => PremiumReceived - PremiumPaid;
        public string ExpirationDate { get; set; }
        public int NumberOfContracts { get; set; }
        public string BoughtOrSold { get; set; }
        public DateTimeOffset Filled { get; set; }
        public double Days { get; set; }
        public int DaysHeld { get; set; }
        public List<Transaction> Transactions { get; set; }
        public bool ExpiresSoon { get; set; }
        public bool IsExpired { get; set; }
        public DateTimeOffset? Closed { get; set; }
        public bool Assigned { get; set; }
        public List<string> Notes { get; }
        public string ItmOtmLabel { get; set; }
        public bool IsFavorable { get; set; }
        public decimal StrikePriceDiff { get; set; }
    }
}