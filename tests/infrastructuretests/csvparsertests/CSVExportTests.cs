using System;
using core.Cryptos;
using core.fs.Services;
using core.fs.Adapters.CSV;
using core.fs.Stocks;
using csvparser;
using testutils;
using Xunit;

namespace csvparsertests
{
    public class CSVExportTests
    {
        private static readonly ICSVWriter _csvWriter = new CsvWriterImpl();
        [Fact]
        public void ExportStocksHeader()
        {
            var ticker = TestDataGenerator.NET;
            
            var stock = StockPosition.buy(
                1m, 100, DateTime.UtcNow,
                StockPosition.openLong(ticker, DateTimeOffset.UtcNow)
            );
            
            var report = CSVExport.stocks(_csvWriter, new[] {stock});

            Assert.Contains("Ticker,Type,Amount,Price,Date", report);
            Assert.Contains(ticker.Value, report);
        }

        [Fact]
        public void ExportCryptos()
        {
            var crypto = new OwnedCrypto("btc", Guid.NewGuid());

            crypto.Purchase(quantity: 1.2m, dollarAmountSpent: 200, date: DateTimeOffset.UtcNow);
            crypto.Sell(quantity: 0.2m, dollarAmountReceived: 100, date: DateTimeOffset.UtcNow);

            var report = CSVExport.cryptos(_csvWriter, new[] {crypto});

            Assert.Contains("Symbol,Type,Amount,Price,Date", report);
            Assert.Contains("BTC", report);
            Assert.Contains("buy", report);
            Assert.Contains("sell", report);
        }

        [Fact]
        public void FilenameEndsWithCsv()
        {
            var filename = CSVExport.generateFilename("option");

            Assert.Contains("option", filename);
            Assert.EndsWith("csv", filename);
        }
    }
}
