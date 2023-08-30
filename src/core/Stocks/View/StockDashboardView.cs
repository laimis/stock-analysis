#nullable enable
using System;
using System.Collections.Generic;

namespace core.Stocks.View
{
    public class StockDashboardView
    {
        public static string Version = "1";
        
        public StockDashboardView() {}
        public StockDashboardView(List<PositionInstance> positions, List<StockViolationView> violations)
        {
            Positions = positions;
            Violations = violations;
        }

        public List<PositionInstance>? Positions { get; }
        public List<StockViolationView>? Violations { get; }
    }

    public struct StockViolationView : IEquatable<StockViolationView>
    {
        public StockViolationView(decimal? currentPrice, string message, decimal numberOfShares, decimal pricePerShare, string ticker)
        {
            CurrentPrice = currentPrice;
            Message = message;
            NumberOfShares = numberOfShares;
            PricePerShare = pricePerShare;
            Ticker = ticker;
        }

        public decimal? CurrentPrice { get; }
        public string Message { get; }
        public decimal NumberOfShares { get; }
        public decimal PricePerShare { get; }
        public string Ticker { get; }

        public bool Equals(StockViolationView other) => Ticker == other.Ticker;

        public override bool Equals(object? obj) => obj is StockViolationView other && Equals(other);

        public override int GetHashCode() => Ticker.GetHashCode();

        public static bool operator ==(StockViolationView lhs, StockViolationView rhs) => lhs.Equals(rhs);

        public static bool operator !=(StockViolationView lhs, StockViolationView rhs) => !(lhs == rhs);
    }
}
#nullable restore