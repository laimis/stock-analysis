using System;
using core.Adapters.Stocks;
using core.Options;

namespace core.Portfolio
{
    public struct ReviewEntry
    {
        public ReviewEntry(OwnedOption o)
        {
            Ticker = o.Ticker;
            Description = o.Description;
            Expiration = o.Expiration;
            IsExpired = o.IsExpired;
            ExpiresSoon = o.ExpiresSoon;
            DaysLeft = o.DaysLeft;
            Stats = null;
            Price = new TickerPrice();
            IsOption = true;
            IsNote = false;
        }
        
        public string Ticker { get; internal set; }
        public string Description { get; internal set; }
        public DateTimeOffset? Expiration { get; internal set; }
        public long? DaysLeft { get; internal set; }
        public bool IsNote { get; internal set; }
        public DateTimeOffset Created { get; internal set; }
        public StockAdvancedStats Stats { get; internal set; }
        public TickerPrice Price { get; internal set; }
        public bool IsExpired { get; internal set; }
        public bool ExpiresSoon { get; internal set; }
        public bool IsOption { get; internal set; }
    }
}