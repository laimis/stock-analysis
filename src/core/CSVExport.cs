using System;
using System.Collections.Generic;
using System.Text;
using core.Options;
using core.Stocks;

namespace core
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

        private static string Generate(
            string header,
            IEnumerable<object> objs,
            Func<object, IEnumerable<object>> rowGenerator)
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
                state.Closed != null ? state.Closed.Value.ToString("yyyy-MM-dd") : null,
                state.Amount,
                state.Premium,
                state.Spent,
                state.Profit
            };
        }

        public static string GetOptionsExportHeaders()
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

        public static string GetStocksExportHeaders()
        {
            return "ticker,spent,earned,purchased,sold,profit";
        }

        public static string GenerateFilename(string exportType)
        {
            return exportType + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}