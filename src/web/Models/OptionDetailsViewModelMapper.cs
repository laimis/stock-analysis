using System;
using System.Collections.Generic;
using System.Linq;
using core.Options;

namespace web.Models
{
    public class OptionDetailsViewModelMapper
    {
        public static OptionDetailsViewModel Map(
            double price,
            IEnumerable<OptionDetail> options)
        {
            var optionList = options
                .Where(o => o.Volume > 0 || o.OpenInterest > 0)
                .Where(o => o.ParsedExpirationDate > DateTime.UtcNow)
                .OrderBy(o => o.ExpirationDate)
                .ToArray();

            var expirations = optionList.Select(o => o.ExpirationDate)
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            var puts = optionList.Where(o => o.IsPut);
            var calls = optionList.Where(o => o.IsCall);

            return new OptionDetailsViewModel {
                StockPrice = price,
                Options = optionList,
                Expirations = expirations,
                Breakdown = new OptionBreakdownViewModel {
                    CallVolume = calls.Sum(o => o.Volume),
                    CallSpend = calls.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnCalls = calls.Average(o => o.Volume * o.StrikePrice) / calls.Average(o => o.Volume),

                    PutVolume = puts.Sum(o => o.Volume),
                    PutSpend = puts.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnPuts = puts.Average(o => o.Volume * o.StrikePrice) / puts.Average(o => o.Volume),
                }
            };
        }
    }
}