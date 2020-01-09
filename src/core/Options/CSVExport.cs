using System;
using System.Collections.Generic;
using System.Text;
using core.Portfolio;

namespace core.Options
{
    public class CSVExport
    {
        public static string Generate(IEnumerable<OwnedStock> stocks)
        {
            return Generate(
                GetStocksExportHeaders(),
                stocks,
                o => GetExportParts((o as OwnedStock).State));
        }

        public static string Generate(IEnumerable<SoldOption> stocks)
        {
            return Generate(
                GetOptionsExportHeaders(),
                stocks,
                o => GetExportParts((o as SoldOption).State));
        }

        private static string Generate(string header, IEnumerable<object> objs, Func<object, object[]> rowGenerator)
        {
            var builder = new StringBuilder();

            builder.AppendLine(header);
            
            foreach (var s in objs)
            {
                builder.AppendLine(string.Join(",", rowGenerator(s)));
            }

            return builder.ToString();
        }

        private static object[] GetExportParts(SoldOptionState state)
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

        private static string GetOptionsExportHeaders()
        {
            return "ticker,strike price,type,expiration,closed,amount,premium,spent,profit";
        }

        private static object[] GetExportParts(OwnedStockState state)
        {
            return new object[]{
                state.Ticker,
                state.Spent,
                state.Earned,
                state.Purchased.ToString("yyyy-MM-dd"),
                state.Sold != null ? state.Sold.Value.ToString("yyyy-MM-dd") : null,
                state.Profit
            };
        }

        private static string GetStocksExportHeaders()
        {
            return "ticker,spent,earned,purchased,sold,profit";
        }

        public static string GenerateFilename(string exportType)
        {
            return exportType + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}