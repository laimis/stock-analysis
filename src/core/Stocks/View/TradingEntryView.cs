using System.Collections.Generic;

namespace core.Stocks.View
{

    public class TradingEntryView
    {
        public TradingEntryView(OwnedStockState state)
        {
            NumberOfShares  = state.Owned;
            AverageCost     = state.AverageCost;
            Ticker          = state.Ticker;
            MaxNumberOfShares = state.CurrentPosition.MaxNumberOfShares;
            Gain            = state.CurrentPosition.Profit;
            StopPrice       = state.CurrentPosition.StopPrice;
            Notes           = state.CurrentPosition.Notes;
            RiskedPct       = state.CurrentPosition.RiskedPct;
        }

        public decimal NumberOfShares { get; }
        public decimal AverageCost { get; }
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