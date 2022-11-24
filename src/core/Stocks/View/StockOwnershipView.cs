using System;
using System.Collections.Generic;

namespace core.Stocks.View
{
    public record class StockOwnershipView(Guid id, PositionInstance currentPosition, string ticker, List<PositionInstance> positions);
}