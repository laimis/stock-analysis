using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core.Adapters.CSV;
using core.Shared;
using CsvHelper;
using CsvHelper.Configuration;

namespace csvparser
{
    public class CSVParser : ICSVParser
    {
        public ServiceResponse<IEnumerable<T>> Parse<T>(string content)
        {
            try
            {
                using var reader = new StringReader(content);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = (header, _) =>
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

                var records = csvReader.GetRecords<T>().ToList(); // doing to list to make sure that the file is read
                // before reader gets disposed
                return new ServiceResponse<IEnumerable<T>>(records);
            }
            catch(HeaderValidationException ex)
            {
                var error = "Missing header fields: " + string.Join(",", ex.HeaderNames);
                return new ServiceResponse<IEnumerable<T>>(new ServiceError(error));
            }
        }
    }
}