using System.Collections.Generic;
using System.Globalization;
using System.IO;
using core.Shared.Adapters.CSV;
using CsvHelper;
using CsvHelper.Configuration;

namespace csvparser
{
    public class CsvWriterImpl : ICSVWriter
    {
        CsvConfiguration _config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ","
        };

        public string Generate<T>(IEnumerable<T> rows)
        {    
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, _config);

            csv.WriteRecords<T>(rows);

            return writer.ToString();
        }
    }
}