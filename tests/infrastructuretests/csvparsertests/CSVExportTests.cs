using System;
using core.Cryptos;
using core.fs.Services;
using core.fs.Adapters.CSV;
using core.fs.Stocks;
using core.Options;
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
        public void ExportOptionsHeader()
        {
            var option = new OwnedOption(
                TestDataGenerator.NET,
                2.5m,
                OptionType.CALL,
                DateTimeOffset.UtcNow.AddDays(1),
                Guid.NewGuid());
            
            option.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var report = CSVExport.options(_csvWriter, new[] {option});

            Assert.Contains("Ticker,Type,Strike,OptionType,Expiration,Amount,Premium,Filled", report);
            
            Assert.Contains(TestDataGenerator.NET.Value, report);
            Assert.Contains("CALL", report);
            Assert.Contains("2.5", report);
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