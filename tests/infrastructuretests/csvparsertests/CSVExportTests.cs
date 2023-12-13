using System;
using core.Cryptos;
using core.fs.Services;
using core.fs.Shared.Adapters.CSV;
using core.fs.Shared.Domain;
using core.Notes;
using core.Options;
using core.Shared;
using core.Stocks;
using coretests.testdata;
using csvparser;
using Microsoft.FSharp.Core;
using Xunit;

namespace csvparsertests
{
    public class CSVExportTests
    {
        private static readonly ICSVWriter _csvWriter = new CsvWriterImpl();
        [Fact]
        public void ExportStocksHeader()
        {
            var stock = StockPosition.buy(
                1m, 100, DateTime.UtcNow, new FSharpOption<string>("some note"),
                StockPosition.openLong(TestDataGenerator.NET, DateTimeOffset.UtcNow)
            );
            
            var report = CSVExport.stocks(_csvWriter, new[] {stock});

            Assert.Contains("Ticker,Type,Amount,Price,Date,Notes", report);
            Assert.Contains(TestDataGenerator.TSLA.Value, report);
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
                new Ticker("tlsa"),
                2.5m,
                OptionType.CALL,
                DateTimeOffset.UtcNow.AddDays(1),
                Guid.NewGuid());
            
            option.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var report = CSVExport.options(_csvWriter, new[] {option});

            Assert.Contains("Ticker,Type,Strike,OptionType,Expiration,Amount,Premium,Filled", report);
            
            Assert.Contains("TLSA", report);
            Assert.Contains("CALL", report);
            Assert.Contains("2.5", report);
        }

        [Fact]
        public void ExportNotes()
        {
            var note = new Note(Guid.NewGuid(), "my note", TestDataGenerator.TSLA, DateTimeOffset.UtcNow);

            var report = CSVExport.notes(_csvWriter, new[] {note});

            Assert.Contains("Created,Ticker,Note", report);
            Assert.Contains(note.State.RelatedToTicker.Value, report);
            Assert.Contains("my note", report);
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