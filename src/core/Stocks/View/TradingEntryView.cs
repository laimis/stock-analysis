using System;
using System.Collections.Generic;

namespace core.Stocks.View
{

    public class TradingEntryView
    {
        public TradingEntryView(OwnedStockState state)
        {
            AverageCost     = state.AverageCost;
            Opened          = state.CurrentPosition.Opened;
            DaysHeld        = state.DaysHeld;
            Gain            = state.CurrentPosition.Profit;
            MaxNumberOfShares = state.CurrentPosition.MaxNumberOfShares;
            Notes           = state.CurrentPosition.Notes;
            NumberOfShares  = state.Owned;
            RiskedPct       = state.CurrentPosition.RiskedPct;
            StopPrice       = state.CurrentPosition.StopPrice;
            Ticker          = state.Ticker;
        }

        public decimal NumberOfShares { get; }
        public decimal AverageCost { get; }
        public DateTimeOffset? Opened { get; }
        public int DaysHeld { get; }
        public string Ticker { get; }
        public decimal MaxNumberOfShares { get; }
        public decimal Price { get; private set; }
        public decimal Gain { get; private set; }
        public decimal? StopPrice { get; }
        public List<string> Notes { get; }
        public decimal RiskedPct { get; }
        public decimal? UnrealizedGain { get; private set; }

        internal void ApplyPrice(decimal currentPrice)
        {
            Price = currentPrice;
            Gain = (Price - AverageCost) / AverageCost;
            UnrealizedGain = (Price - AverageCost) * NumberOfShares;
        }

        public decimal RR => (Price - AverageCost) / AverageCost / RiskedPct;
        public decimal PotentialLoss => NumberOfShares * AverageCost * RiskedPct;
    }
}