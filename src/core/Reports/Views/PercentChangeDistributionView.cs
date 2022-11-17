using core.Stocks.Services.Analysis;

namespace core.Reports.Views
{
    public record struct PercentChangeStatisticsView(string ticker, DistributionStatistics recent, DistributionStatistics allTime);
}