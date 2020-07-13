using System;
using core.Options;
using core.Portfolio;

namespace core.Admin
{
    internal class WeeklyReviewEntryView
    {
        public WeeklyReviewEntryView((ReviewEntryGroup p, ReviewEntry re) pair)
        {
            Ticker = pair.p.Ticker;
            Price = pair.p.Price.Amount;
            Cost = String.Format("{0:0.00}", pair.re.AverageCost);
            GainsPct = CalcGainPct(pair.p.Price.Amount, pair.re);
            ItmOtmLabel = CalcItmOtm(pair.p, pair.re);
            PptionType = pair.re.OptionType.ToString();
            StrikePrice = pair.re.StrikePrice;
            Expiration = pair.re.Expiration.HasValue ? pair.re.Expiration.Value.ToString("MMM, dd") : null;
            Earnings = pair.p.EarningsWarning ? pair.p.EarningsDate.Value.ToString("MMM, dd") : null;
        }

        public string Ticker { get; }
        public double Price { get; }
        public string Cost { get; }
        public object GainsPct { get; }
        public object ItmOtmLabel { get; }
        public string PptionType { get; }
        public double StrikePrice { get; }
        public string Expiration { get; }
        public string Earnings { get; }

        private static object CalcItmOtm(ReviewEntryGroup p, ReviewEntry re)
        {
            if (re.OptionType != null)
            {
                return OwnedOptionSummary.GetItmOtmLabel(p.Price.Amount, re.OptionType.Value, re.StrikePrice);
            }

            return null;
        }

        private static object CalcGainPct(double current, ReviewEntry re)
        {
            if (re.AverageCost == 0)
            {
                return "";
            }

            var gains = Math.Round((current - re.AverageCost)/re.AverageCost * 100, 2);

            var plusOrMinus = gains >= 0 ? "+" : "-";
            
            gains = Math.Abs(gains);

            var formatted = String.Format("{0:0.00} %", gains);

            return $"{plusOrMinus} {formatted}";
        }
    }
}