using System.Collections.Generic;
using core.Stocks.Services.Analysis;

namespace core.Reports.Views
{
    public record struct GapsView(List<Gap> gaps, string ticker);
}