namespace core.fs.Admin

    open System
    open core.Account
    open core.Notes
    open core.Options
    open core.Shared
    open core.Shared.Adapters.CSV
    open core.Stocks
    open core.fs.Shared
    open core.fs.Shared.Adapters.Storage

    module Users =
        
        type Query = {
            everyone: bool
        }
        
        type QueryResponse(user:User, stocks:OwnedStock seq, options:OwnedOption seq, notes:Note seq) =
            let stockLength = stocks |> Seq.length
            let optionLength = options |> Seq.length
            let noteLength = notes |> Seq.length
            
            member _.Email = user.State.Email
            member _.Id = user.State.Id
            member _.FirstName = user.State.Firstname
            member _.LastName = user.State.Lastname
            member _.Verified = user.State.Verified.HasValue
            member _.Stocks = stockLength
            member _.Options = optionLength
            member _.Notes = noteLength
        
        type Export = struct end
        
        type Handler(storage:IAccountStorage, portfolio:IPortfolioStorage, csvWriter:ICSVWriter) =
            
            let buildQueryResponse userId =
                async {
                    let! user = storage.GetUser(userId) |> Async.AwaitTask
                    let! options = portfolio.GetOwnedOptions(userId) |> Async.AwaitTask
                    let! notes = portfolio.GetNotes(userId) |> Async.AwaitTask
                    let! stocks = portfolio.GetStocks(userId) |> Async.AwaitTask
                    
                    return QueryResponse(user, stocks, options, notes)
                }
                
            interface IApplicationService
            
            member _.Handle (_:Query) =
                task {
                    let! users = storage.GetUserEmailIdPairs()
                    
                    let! result = 
                        users
                        |> Seq.map (fun emailId -> async {
                            return! emailId.Id |> Guid |> buildQueryResponse
                        })
                        |> Async.Parallel
                        |> Async.StartAsTask
                        
                    return ServiceResponse<QueryResponse seq>(result)
                }
                
            member _.Handle (_:Export) = task {
                let! pairs = storage.GetUserEmailIdPairs()
                
                let! users = 
                    pairs
                    |> Seq.map (fun emailId -> async {
                        return! emailId.Id |> Guid |> storage.GetUser |> Async.AwaitTask
                    })
                    |> Async.Parallel
                    |> Async.StartAsTask
                    
                let filename = CSVExport.GenerateFilename("users")
                
                return ExportResponse(filename, CSVExport.Generate(csvWriter, users)) |> ResponseUtils.success<ExportResponse>
            }