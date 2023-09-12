namespace core.fs.Options

open core.Shared.Adapters.CSV
open core.Shared.Adapters.Storage
open core.fs

module Export =

    type Query(userId:System.Guid) =
        member _.UserId = userId
        member _.Filename = "options"

    type Handler(storage:IPortfolioStorage, csvWriter:ICSVWriter) =
        interface IApplicationService

        member _.Handle(request:Query) =
            task {
                let! options = request.UserId |> storage.GetOwnedOptions;

                let csv = CSVExport.Generate(csvWriter, options);

                return ExportResponse(
                    request.Filename |> CSVExport.GenerateFilename,
                    csv
                );
            }