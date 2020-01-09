using System;
using System.Collections.Generic;
using System.Text;
using core.Portfolio;

namespace core.Options
{
    public class CSVExport
    {
        public static string Generate(IEnumerable<SoldOption> stocks)
        {
            var builder = new StringBuilder();

            builder.AppendLine(GetExportHeaders());
            
            foreach (var s in stocks)
            {
                builder.AppendLine(string.Join(",", GetExportParts(s.State)));
            }

            return builder.ToString();
        }

        public static object[] GetExportParts(SoldOptionState state)
        {
            return new object[]{
                state.Ticker,
                state.StrikePrice,
                state.Type.ToString(),
                state.Expiration.ToString("yyyy-MM-dd"),
                state.Closed.ToString("yyyy-MM-dd"),
                state.Amount,
                state.Premium,
                state.Spent,
                state.Profit
            };
        }

        private static string GetExportHeaders()
        {
            return "ticker,strike price,type,expiration,closed,amount,premium,spent,profit";
        }
    }
}