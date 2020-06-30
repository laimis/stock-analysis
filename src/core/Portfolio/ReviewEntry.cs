using System;
using System.Collections.Generic;
using core.Adapters.Stocks;
using core.Alerts;
using core.Options;
using core.Stocks;

namespace core.Portfolio
{
    public struct ReviewEntry
    {
        public ReviewEntry(OwnedOption o)
        {
            Created = null;
            Ticker = o.Ticker;
            Description = o.Description;
            Expiration = o.Expiration;
            IsExpired = o.IsExpired;
            ExpiresSoon = o.ExpiresSoon;
            DaysLeft = o.DaysLeft;
            Stats = null;
            IsOption = true;
            IsAlert = false;
            PricePoints = new List<AlertPricePoint>();
        }

        public ReviewEntry(OwnedStock s)
        {
            Created = null;
            Ticker = s.Ticker;
            Description = s.Description;
            Expiration = null;
            IsExpired = false;
            ExpiresSoon = false;
            DaysLeft = null;
            Stats = null;
            IsOption = false;
            IsAlert = false;
            PricePoints = new List<AlertPricePoint>();
        }

        public ReviewEntry(Alert a)
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
            PricePoints = a.PricePoints;
        }
        
        public string Ticker { get; }
        public string Description { get; }
        public DateTimeOffset? Expiration { get; }
        public long? DaysLeft { get; }
        public bool IsAlert { get; }
        public DateTimeOffset? Created { get; }
        public StockAdvancedStats Stats { get; }
        public List<AlertPricePoint> PricePoints { get; }
        public bool IsExpired { get; }
        public bool ExpiresSoon { get; }
        public bool IsOption { get; }
    }
}