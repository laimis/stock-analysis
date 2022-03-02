using System;
using core;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Stocks;
using Xunit;

namespace coretests
{
    public class CSVExportTests
    {
        [Fact]
        public void ExportStocksHeader()
        {
            var stock = new OwnedStock("tsla", Guid.NewGuid());
            stock.Purchase(1, 100, DateTime.UtcNow, "some note");
            
            var report = CSVExport.Generate(new[] {stock});

            Assert.Contains(CSVExport.STOCK_HEADER, report);
            Assert.Contains("TSLA", report);
        }

        [Fact]
        public void ExportCryptos()
        {
            var crypto = new OwnedCrypto("btc", Guid.NewGuid());

            crypto.Purchase(quantity: 1.2m, dollarAmountSpent: 200, date: DateTimeOffset.UtcNow);
            crypto.Sell(quantity: 0.2m, dollarAmountReceived: 100, date: DateTimeOffset.UtcNow);

            var report = CSVExport.Generate(new[] {crypto});

            Assert.Contains(CSVExport.CRYPTOS_HEADER, report);
            Assert.Contains("BTC", report);
            Assert.Contains("buy", report);
            Assert.Contains("sell", report);
        }

        [Fact]
        public void ExportOptionsHeader()
        {
            var option = new OwnedOption(
                "tlsa",
                2.5m,
                OptionType.CALL,
                DateTimeOffset.UtcNow.AddDays(1),
                Guid.NewGuid());
            
            option.Sell(1, 20, DateTimeOffset.UtcNow, "some note");

            var report = CSVExport.Generate(new[] {option});

            Assert.Contains(CSVExport.OPTION_HEADER, report);
            
            Assert.Contains("TLSA", report);
            Assert.Contains("CALL", report);
            Assert.Contains("2.5", report);
        }

        [Fact]
        public void ExportNotes()
        {
            var note = new Note(Guid.NewGuid(), "my note", "stockticker", DateTimeOffset.UtcNow);

            var report = CSVExport.Generate(new[] {note});

            Assert.Contains(CSVExport.NOTE_HEADER, report);
            Assert.Contains(note.State.RelatedToTicker, report);
            Assert.Contains("my note", report);
        }

        [Fact]
        public void FilenameEndsWithCSV()
        {
            var filename = CSVExport.GenerateFilename("option");

            Assert.Contains("option", filename);
            Assert.EndsWith("csv", filename);
        }
    }
}