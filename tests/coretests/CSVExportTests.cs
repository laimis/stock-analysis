using System;
using core;
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
            var stock = new OwnedStock("ticker", "userid");
            stock.Purchase(1, 100, DateTime.UtcNow);

            var report = CSVExport.Generate(new[] {stock});

            Assert.Contains(CSVExport.STOCK_HEADER, report);
            Assert.Contains("ticker", report);
            Assert.Contains("ticker", report);
        }

        [Fact]
        public void ExportOptionsHeader()
        {
            var option = new SoldOption("ticker", OptionType.CALL, DateTimeOffset.UtcNow.AddDays(1), 2.5, "user", 1, 100, DateTimeOffset.UtcNow);

            var report = CSVExport.Generate(new[] {option});

            Assert.Contains(CSVExport.OPTION_HEADER, report);
            
            Assert.Contains("ticker", report);
            Assert.Contains("CALL", report);
            Assert.Contains("2.5", report);
        }

        [Fact]
        public void ExportNotes()
        {
            var note = new Note("user", "note", "ticker", 100);

            var report = CSVExport.Generate(new[] {note});

            Assert.Contains(CSVExport.NOTE_HEADER, report);
            Assert.Contains("ticker", report);
            Assert.Contains("100", report);
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