using System.Collections.Generic;
using core.Stocks.Services;

namespace core.Reports.Views
{
    public record struct GapsView(string ticker, List<Gap> gaps);
}