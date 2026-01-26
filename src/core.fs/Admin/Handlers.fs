namespace core.fs.Admin

open core.fs
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Adapters.Email
open core.fs.Adapters.Storage
open core.fs.Options
open core.fs.Services
open core.fs.Services.SECTickerSyncService
open core.fs.Stocks

type Query = {
    everyone: bool
}

type TriggerSECTickerSync = struct end
        
type QueryResponse(user:User, stocks:StockPositionState seq, options:OptionPositionState seq) =
    let stockLength = stocks |> Seq.length
    let optionLength = options |> Seq.length
    
    member _.Email = user.State.Email
    member _.Id = user.State.Id
    member _.FirstName = user.State.Firstname
    member _.LastName = user.State.Lastname
    member _.Verified = user.State.Verified.HasValue
    member _.Stocks = stockLength
    member _.Options = optionLength
        
type Export = struct end

type SendWelcomeEmail = {
    userId:UserId
}

type SendEmail = {
    input:EmailInput
}
        
type Handler(storage:IAccountStorage, email:IEmailService, portfolio:IPortfolioStorage, csvWriter:ICSVWriter, secTickerSync:SECTickerSyncService) =
            
    let buildQueryResponse userId =
        async {
            let! user = storage.GetUser(userId) |> Async.AwaitTask
            let! options = portfolio.GetOptionPositions(userId) |> Async.AwaitTask
            let! stocks = portfolio.GetStockPositions(userId) |> Async.AwaitTask
            
            return QueryResponse(user.Value, stocks, options)
        }
                
    interface IApplicationService
    
    member _.Handle (cmd:SendEmail) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        let! emailResult = cmd.input |> email.SendWithInput
        match emailResult with
        | Ok () -> return Ok ()
        | Error err -> return ServiceError err |> Error
    }
    
    member _.Handle sendWelcome : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        let! user = sendWelcome.userId |> storage.GetUser 
        match user with
        | Some user ->
            let! emailResult = email.SendWelcome (Recipient(email=user.State.Email, name=user.State.Name)) Sender.Support {||}
            match emailResult with
            | Ok () -> return Ok ()
            | Error err -> return ServiceError err |> Error
            
        | None -> return "User not found" |> ServiceError |> Error
        
    }
            
    member _.Handle (_:Query) : System.Threading.Tasks.Task<Result<QueryResponse array,ServiceError>> =
        task {
            let! users = storage.GetUserEmailIdPairs()
            
            let! result = 
                users
                |> Seq.map (fun emailId -> emailId.Id |> buildQueryResponse)
                |> Async.Parallel
                |> Async.StartAsTask
                
            return result |> Ok
        }
        
    member _.Handle (_:Export) = task {
        let! pairs = storage.GetUserEmailIdPairs()
        
        let! userTasks = 
            pairs
            |> Seq.map (fun emailId ->
                emailId.Id |> storage.GetUser |> Async.AwaitTask
            )
            |> Async.Parallel
            |> Async.StartAsTask
            
        let users = userTasks |> Seq.choose id
            
        let filename = CSVExport.generateFilename "users"
        
        return ExportResponse(filename, CSVExport.users csvWriter users)
    }
    
    member _.Handle (_:TriggerSECTickerSync) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        do! secTickerSync.Execute()
        return Ok ()
    }
