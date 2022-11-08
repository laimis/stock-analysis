using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    public record class StockOwnershipView(Guid id, PositionInstance position, string ticker, List<Transaction> transactions);
}