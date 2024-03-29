using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core.fs;
using core.fs.Adapters.CSV;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.FSharp.Core;

namespace csvparser
{
    public class CSVParser : ICSVParser
    {
        public FSharpResult<IEnumerable<T>,ServiceError> Parse<T>(string content)
        {
            try
            {
                using var reader = new StringReader(content);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = (header) =>
                    {
                        // remove spaces and capitalize the first letter so that
                        // CSVs produced by various services like Coinbase, TD Ameritrade, etc.
                        // can be parsed correctly
                        var spacesRemoved = Regex.Replace(header.Header, @"\s", string.Empty);
                        return Char.ToUpper(spacesRemoved[0]) + spacesRemoved.ToLower().Substring(1);
                    },
                    ShouldSkipRecord = (arr) => arr.Row.ColumnCount == 0 || arr.Row.GetField(0) == "***END OF FILE***"
                    // TD Ameritrade includes **END OF FILE** at the end
                };
                    
                var csvReader = new CsvReader(reader, config);

                var records = csvReader.GetRecords<T>().ToList(); // doing to list to make sure that the file is read
                // before reader gets disposed
                return FSharpResult<IEnumerable<T>, ServiceError>.NewOk(records);
            }
            catch(HeaderValidationException ex)
            {
                var error = "Header validation failed: " + ex.Message;
                return FSharpResult<IEnumerable<T>, ServiceError>.NewError(new ServiceError(error));
            }
        }
    }
}
