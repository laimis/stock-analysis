namespace core.fs.Options

module Export =
    open core.fs
    open core
    open core.Shared.Adapters.CSV

    type Query(userId:System.Guid) =
        member _.UserId = userId
        member _.Filename = "options"

    type Handler(storage:IPortfolioStorage, csvWriter:ICSVWriter) =
        interface IApplicationService

        member _.Handle(request:Query) =
            task {
                let! options = request.UserId |> storage.GetOwnedOptions;

                let csv = CSVExport.Generate(csvWriter, options);

                return new ExportResponse(
                    request.Filename |> CSVExport.GenerateFilename,
                    csv
                );
            }