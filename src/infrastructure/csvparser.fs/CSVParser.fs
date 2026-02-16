namespace csvparser.fs

open System
open System.IO
open System.Globalization
open System.Text.RegularExpressions
open CsvHelper
open CsvHelper.Configuration
open core.fs
open core.fs.Adapters.CSV

type CSVParser() =
    interface ICSVParser with
        member _.Parse<'T>(content: string) : Result<seq<'T>, ServiceError> =
            try
                use reader = new StringReader(content)
                let config = 
                    CsvConfiguration(
                        CultureInfo.InvariantCulture,
                        PrepareHeaderForMatch = (fun args ->
                            // remove spaces and capitalize the first letter so that
                            // CSVs produced by various services like Coinbase, TD Ameritrade, etc.
                            // can be parsed correctly
                            let spacesRemoved = Regex.Replace(args.Header, @"\s", String.Empty)
                            if spacesRemoved.Length > 0 then
                                Char.ToUpper(spacesRemoved.[0]).ToString() + spacesRemoved.ToLower().Substring(1)
                            else
                                spacesRemoved),
                        ShouldSkipRecord = (fun args ->
                            args.Row.ColumnCount = 0 || args.Row.GetField(0) = "***END OF FILE***")
                            // TD Ameritrade includes ***END OF FILE*** at the end
                    )
                
                use csvReader = new CsvReader(reader, config)
                
                // doing to list to make sure that the file is read
                // before reader gets disposed
                let records = csvReader.GetRecords<'T>() |> Seq.toList
                Ok (records :> seq<'T>)
            with
            | :? HeaderValidationException as ex ->
                let error = "Header validation failed: " + ex.Message
                Error (ServiceError(error))
            | ex ->
                Error (ServiceError(ex.Message))
