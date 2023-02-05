using System.Collections.Generic;
using core.Stocks.Services.Analysis;

namespace core.Reports.Views
{
    public record struct PercentChangeStatisticsView(string ticker, DistributionStatistics recent, DistributionStatistics allTime);
    public record struct GapsView(List<Gap> gaps, string ticker);
    public record struct DailyOutcomeScoresReportView(List<DateScorePair> dailyScores, string ticker);
}