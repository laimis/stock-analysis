using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core.Adapters.CSV;
using CsvHelper;
using CsvHelper.Configuration;

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
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        PrepareHeaderForMatch = (header, pos) =>
                        {
                            // remove spaces and capitalize the first letter so that
                            // CSVs produced by various services like Coinbase, TD Ameritrade, etc.
                            // can be parsed correctly
                            var spacesRemoved = Regex.Replace(header, @"\s", string.Empty);
                            return Char.ToUpper(spacesRemoved[0]) + spacesRemoved.ToLower().Substring(1);
                        },
                        ShouldSkipRecord = (arr) => arr.Length == 0 || arr[0] == "***END OF FILE***"
                        // TD Ameritrade includes **END OF FILE** at the end
                    };
                    
                    var csvReader = new CsvReader(reader, config);
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