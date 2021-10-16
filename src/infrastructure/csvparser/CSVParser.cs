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
                            var spacesRemoved = Regex.Replace(header, @"\s", string.Empty);

                            // capitalize first
                            var result = Char.ToUpper(spacesRemoved[0]) + spacesRemoved.ToLower().Substring(1);

                            Console.WriteLine("Prepared: " + result);

                            return result;
                        },
                        ShouldSkipRecord = (arr) => arr.Length == 0 || arr[0] == "***END OF FILE***"
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