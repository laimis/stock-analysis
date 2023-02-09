#nullable enable
using core.Adapters.Stocks;

namespace core.Stocks.View
{
    public record struct StockDetailsView(string Ticker, decimal? Price, StockProfile? Profile);
}
#nullable restore