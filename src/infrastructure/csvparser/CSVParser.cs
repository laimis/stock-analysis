using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using core.Adapters.CSV;
using CsvHelper;

namespace csvparser
{
    public class CSVParser : ICSVParser
    {
        public (IEnumerable<T>, string error) Parse<T>(string content)
        {
            try
            {
                using (var reader = new StringReader(content))
                {
                    var csvReader = new CsvReader(reader, CultureInfo.CurrentCulture);
                    return (csvReader.GetRecords<T>().ToList(), null);
                }
            }
            catch(HeaderValidationException ex)
            {
                return (null, "Missing header fields: " + string.Join(",", ex.HeaderNames));
            }
        }
    }
}