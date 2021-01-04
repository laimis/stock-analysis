using System;
using core.Options;
using core.Portfolio;

namespace core.Admin
{
    internal class WeeklyReviewEntryView
    {
        public WeeklyReviewEntryView((ReviewEntryGroup p, ReviewEntry re) pair)
        {
            ticker = pair.p.Ticker;
            price = pair.p.Price.Amount;
            cost = String.Format("{0:0.00}", pair.re.AverageCost);
            gainsPct = CalcGainPct(pair.p.Price.Amount, pair.re);
            itmOtmLabel = CalcItmOtm(pair.p, pair.re);
            optionType = pair.re.OptionType.ToString();
            strikePrice = pair.re.StrikePrice;
            expiration = pair.re.Expiration.HasValue ? pair.re.Expiration.Value.ToString("MMM, dd") : null;
            earnings = pair.p.EarningsWarning ? pair.p.EarningsDate.Value.ToString("MMM, dd") : null;
        }

        public string ticker { get; }
        public double price { get; }
        public string cost { get; }
        public object gainsPct { get; }
        public object itmOtmLabel { get; }
        public string optionType { get; }
        public double strikePrice { get; }
        public string expiration { get; }
        public string earnings { get; }

        private static object CalcItmOtm(ReviewEntryGroup p, ReviewEntry re)
        {
            if (re.OptionType != null)
            {
                return OwnedOptionView.GetItmOtmLabel(p.Price.Amount, re.OptionType.Value, re.StrikePrice);
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