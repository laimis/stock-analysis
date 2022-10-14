using System.Collections.Generic;
using core.Portfolio;
using core.Stocks.Services;

namespace core.Reports.Views
{
    public record struct DailyPortfolioReportCategory(
        string name,
        OutcomeType type,
        List<PositionAnalysisEntry> analysis
    );

    public record struct DailyPortfolioReportView(
        IEnumerable<DailyPortfolioReportCategory> categories
    );
}