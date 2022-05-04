using System;

namespace core.Stocks.View
{
    public class StockTradeView
    {
        public  StockTradeView(){}
        public StockTradeView(PositionInstance position)
        {
            Ticker = position.Ticker;
            Opened = position.Opened;
            Closed = position.Closed;
            DaysHeld = position.DaysHeld;
            MaxNumberOfShares = position.MaxNumberOfShares;
            MaxCost = position.MaxCost;
            Profit = position.Profit;
            ReturnPct = position.Percentage;
            NumberOfBuys = position.NumberOfBuys;
            NumberOfSells = position.NumberOfSells;
        }

        public string Ticker { get; set; }
        public DateTimeOffset? Opened { get; }
        public DateTimeOffset? Closed { get; }
        public int DaysHeld { get; }
        public decimal MaxNumberOfShares { get; }
        public decimal MaxCost { get; }
        public decimal Profit { get; }
        public decimal ReturnPct { get; }
        public int NumberOfBuys { get; }
        public int NumberOfSells { get; }
    }
}