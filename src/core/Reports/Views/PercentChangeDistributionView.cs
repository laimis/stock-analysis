using core.Stocks.Services;

namespace core.Reports.Views
{
    public record struct PercentChangeStatisticsView(string ticker, PercentChangeDescriptor recent, PercentChangeDescriptor allTime);
}