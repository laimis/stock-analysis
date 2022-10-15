using System.Collections.Generic;
using core.Portfolio;
using core.Stocks.Services;

namespace core.Reports.Views
{
    public record struct PortfolioReportCategory(
        string name,
        OutcomeType type,
        List<PositionAnalysisEntry> analysis
    );

    public record struct PortfolioReportView(
        IEnumerable<PortfolioReportCategory> categories
    );
}