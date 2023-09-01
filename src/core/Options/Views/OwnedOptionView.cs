using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Options;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionView
    {
        public OwnedOptionView(OwnedOptionState o, OptionDetail optionDetail)
        {
            Id = o.Id;
            Ticker = o.Ticker;
            OptionType = o.OptionType.ToString();
            StrikePrice = o.StrikePrice;
            ExpirationDate = o.ExpirationDate;
            NumberOfContracts = Math.Abs(o.NumberOfContracts);
            BoughtOrSold = o.SoldToOpen.Value ? "Sold" : "Bought";
            Filled = o.FirstFill.Value;
            Days = o.Days;
            DaysHeld = o.DaysHeld;
            Transactions = o.Transactions.Where(t => !t.IsPL).ToList();
            ExpiresSoon = o.ExpiresSoon;
            IsExpired = o.IsExpired;
            Closed = o.Closed;
            Assigned = o.Assigned;
            Notes = o.Notes;
            Detail = optionDetail;

            var credits = o.Transactions.Where(t => !t.IsPL && t.Amount >= 0);
            var debits = o.Transactions.Where(t => !t.IsPL && t.Amount < 0);

            if (credits.Any()) PremiumReceived = credits.Sum(t => t.Amount);
            if (debits.Any()) PremiumPaid = Math.Abs(debits.Sum(t => t.Amount));

            if (optionDetail?.UnderlyingPrice != null)
            {
                CurrentPrice = optionDetail.UnderlyingPrice.Value;
                ItmOtmLabel = GetItmOtmLabel(CurrentPrice, OptionType, StrikePrice);
                IsFavorable = GetIsFavorable();

                if (CurrentPrice > 0)
                {
                    StrikePriceDiff = Math.Abs(StrikePrice - CurrentPrice) / CurrentPrice;
                }
            }
        }

        private bool GetIsFavorable()
        {
            return _favorableLogic[BoughtOrSold](ItmOtmLabel);
        }

        private static readonly Dictionary<string, Func<string, bool>> _favorableLogic = new()
        {
            { "Bought", (otm) => otm != "OTM"},
            { "Sold", (otm) => otm == "OTM"}
        };

        private static readonly Dictionary<string, Func<decimal, decimal, string>> _otmLogic = new()
        {
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

        public Guid Id { get; }
        public string Ticker { get; }
        public decimal CurrentPrice { get; }
        public string OptionType { get; }
        public decimal StrikePrice { get; }
        public decimal PremiumReceived { get; }
        public decimal PremiumPaid { get; }
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
        public string ExpirationDate { get; }
        public int NumberOfContracts { get; }
        public string BoughtOrSold { get; }
        public DateTimeOffset Filled { get; }
        public double Days { get; }
        public int DaysHeld { get; }
        public List<Transaction> Transactions { get; }
        public bool ExpiresSoon { get; }
        public bool IsExpired { get; }
        public DateTimeOffset? Closed { get; }
        public bool Assigned { get; }
        public List<string> Notes { get; }
        public string ItmOtmLabel { get; }
        public bool IsFavorable { get; }
        public decimal StrikePriceDiff { get; }
        public OptionDetail Detail { get; }
    }
}