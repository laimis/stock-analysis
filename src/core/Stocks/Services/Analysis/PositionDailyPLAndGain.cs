using System.Collections.Generic;
using core.Shared;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class PositionDailyPLAndGain
    {
        public static (ChartDataPointContainer<decimal> profit, ChartDataPointContainer<decimal> gainPct) Generate(
            PriceBar[] bars, 
            PositionInstance position)
        {
            // find the first bar that is after the position was opened
            var firstBar = 0;
            for (var i = 0; i < bars.Length; i++)
            {
                if (bars[i].Date >= position.Opened)
                {
                    firstBar = i;
                    break;
                }
            }

            // find the last bar that is before the position was closed
            var lastBar = bars.Length - 1;
            if (position.Closed != null)
            {
                for (var i = bars.Length - 1; i >= 0; i--)
                {
                    if (bars[i].Date <= position.Closed.Value)
                    {
                        lastBar = i;
                        break;
                    }
                }
            }

            var profit = new ChartDataPointContainer<decimal>(label: "Profit", DataPointChartType.Line);
            var gainPct = new ChartDataPointContainer<decimal>(label: "Gain %", DataPointChartType.Line);

            var shares = position.CompletedPositionShares;
            var costBasis = position.AverageBuyCostPerShare;
            
            for (var i = firstBar; i <= lastBar; i++)
            {
                var bar = bars[i];

                var currentPrice = bar.High;
                var currentGainPct = (currentPrice - costBasis) / costBasis * 100;
                var currentProfit = shares * (currentPrice - costBasis);

                profit.Add(bar.Date, currentProfit);
                gainPct.Add(bar.Date, currentGainPct);
            }

            return (profit, gainPct);
        }
    }
}