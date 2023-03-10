#nullable enable
using System;
using System.Collections.Generic;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.View
{
    public class StockDashboardView : IViewModel
    {
        public StockDashboardView() {}
        public StockDashboardView(List<PositionInstance> positions)
        {
            Positions = positions;
            Calculated = DateTimeOffset.UtcNow;
        }

        public List<PositionInstance>? Positions { get; set; }
        public DateTimeOffset Calculated { get; set; }
        public List<StockViolationView>? Violations { get; private set; }

        internal void SetViolations(List<StockViolationView> violations)
        {
            Violations = violations;
        }
    }

    public struct StockViolationView : IEquatable<StockViolationView>
    {
        public StockViolationView(string message, decimal numberOfShares, decimal pricePerShare, string ticker)
        {
            Message = message;
            NumberOfShares = numberOfShares;
            PricePerShare = pricePerShare;
            Ticker = ticker;
        }

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