namespace csvparser.fs

open System.IO
open System.Globalization
open CsvHelper
open CsvHelper.Configuration
open core.fs.Adapters.CSV

type CsvWriterImpl() =
    let config = 
        CsvConfiguration(CultureInfo.InvariantCulture,
            HasHeaderRecord = true,
            Delimiter = ","
        )
    
    interface ICSVWriter with
        member _.Generate<'T>(rows: seq<'T>) : string =
            use writer = new StringWriter()
            use csv = new CsvWriter(writer, config)
            
            csv.WriteRecords(rows)
            
            writer.ToString()
