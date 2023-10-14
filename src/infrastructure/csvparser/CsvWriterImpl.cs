using System.Collections.Generic;
using System.Globalization;
using System.IO;
using core.fs.Shared.Adapters.CSV;
using CsvHelper;
using CsvHelper.Configuration;

namespace csvparser
{
    public class CsvWriterImpl : ICSVWriter
    {
        private readonly CsvConfiguration _config = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ","
        };

        public string Generate<T>(IEnumerable<T> rows)
        {    
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, _config);

            csv.WriteRecords(rows);

            return writer.ToString();
        }
    }
}