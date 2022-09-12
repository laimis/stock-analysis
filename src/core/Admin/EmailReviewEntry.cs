using System;
using System.Collections.Generic;
using core.Adapters.Stocks;
using core.Alerts;
using core.Options;
using core.Stocks;

namespace core.Admin
{
    public struct EmailReviewEntry
    {
        public EmailReviewEntry(OwnedOption o)
        {
            Created = null;
            Ticker = o.State.Ticker;
            Description = $"{Math.Abs(o.State.NumberOfContracts)} ${o.State.StrikePrice} {o.State.OptionType} contracts";
            OptionType = o.State.OptionType;
            Expiration = o.State.Expiration;
            IsExpired = o.State.IsExpired;
            ExpiresSoon = o.State.ExpiresSoon;
            DaysLeft = o.State.DaysLeft;
            Stats = null;
            IsOption = true;
            IsAlert = false;
            AverageCost = 0;
            OptionType = o.State.OptionType;
            StrikePrice = o.State.StrikePrice;
            PricePoints = new List<AlertPricePoint>();
        }

        public EmailReviewEntry(PositionInstance position)
        {
            Created = null;
            Ticker = position.Ticker;
            Description = position.Ticker;
            Expiration = null;
            IsExpired = false;
            ExpiresSoon = false;
            DaysLeft = null;
            Stats = null;
            IsOption = false;
            IsAlert = false;
            AverageCost = position.AverageCostPerShare;
            OptionType = null;
            StrikePrice = 0;
            OptionType = null;
            PricePoints = new List<AlertPricePoint>();
        }

        public EmailReviewEntry(Alert a)
        {
            Ticker = a.Ticker;
            IsAlert = true;
            Created = a.State.Created;
            Description = "Alert";
            Expiration = null;
            DaysLeft = null;
            IsExpired = false;
            ExpiresSoon = false;
            IsOption = false;
            Stats = null;
            AverageCost = 0;
            OptionType = null;
            StrikePrice = 0;
            OptionType = null;
            PricePoints = a.PricePoints;
        }
        
        public string Ticker { get; }
        public string Description { get; }
        public DateTimeOffset? Expiration { get; }
        public long? DaysLeft { get; }
        public bool IsAlert { get; }
        public decimal AverageCost { get; }
        public DateTimeOffset? Created { get; }
        public StockAdvancedStats Stats { get; }
        public List<AlertPricePoint> PricePoints { get; }
        public bool IsExpired { get; }
        public bool ExpiresSoon { get; }
        public bool IsOption { get; }
        public OptionType? OptionType { get; internal set; }
        public decimal StrikePrice { get; internal set; }
    }
}