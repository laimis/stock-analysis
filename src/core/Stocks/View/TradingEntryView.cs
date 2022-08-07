﻿using System.Collections.Generic;

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
            MaxCost         = state.CurrentPosition.MaxCost;
            Gain            = state.CurrentPosition.Profit;
            StopPrice       = state.CurrentPosition.StopPrice;
            Notes           = state.CurrentPosition.Notes;
        }

        public decimal NumberOfShares { get; }
        public decimal AverageCost { get; }
        public string Ticker { get; }
        public decimal MaxNumberOfShares { get; }
        public decimal MaxCost { get; }
        public decimal Price { get; private set; }
        public decimal Gain { get; private set; }
        public decimal? StopPrice { get; }
        public List<string> Notes { get; }

        internal void ApplyPrice(decimal currentPrice)
        {
            Price = currentPrice;
            Gain = (Price - AverageCost) / AverageCost;
        }
    }
}