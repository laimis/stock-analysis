using System;
using core.Adapters.Stocks;
using core.Notes;
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
            Price = new TickerPrice();
            IsOption = true;
            IsNote = false;
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
            Price = new TickerPrice();
            IsOption = false;
            IsNote = false;
        }

        public ReviewEntry(Note n)
        {
            Ticker = n.State.RelatedToTicker;
            IsNote = true;
            Description = n.State.Note;
            Created = n.State.Created;
            Stats = n.State.Stats;
            Price = n.State.Price;
            Expiration = null;
            DaysLeft = null;
            IsExpired = false;
            ExpiresSoon = false;
            IsOption = false;
        }
        
        public string Ticker { get; }
        public string Description { get; }
        public DateTimeOffset? Expiration { get; }
        public long? DaysLeft { get; }
        public bool IsNote { get; }
        public DateTimeOffset? Created { get; }
        public StockAdvancedStats Stats { get; }
        public TickerPrice Price { get; }
        public bool IsExpired { get; }
        public bool ExpiresSoon { get; }
        public bool IsOption { get; }
    }
}