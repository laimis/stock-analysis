namespace core.fs.Admin

open core.Options
open core.fs
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Adapters.Email
open core.fs.Adapters.Storage
open core.fs.Services
open core.fs.Stocks

type Query = {
    everyone: bool
}
        
type QueryResponse(user:User, stocks:StockPositionState seq, options:OwnedOption seq) =
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
        
type Handler(storage:IAccountStorage, email:IEmailService, portfolio:IPortfolioStorage, csvWriter:ICSVWriter) =
            
    let buildQueryResponse userId =
        async {
            let! user = storage.GetUser(userId) |> Async.AwaitTask
            let! options = portfolio.GetOwnedOptions(userId) |> Async.AwaitTask
            let! stocks = portfolio.GetStockPositions(userId) |> Async.AwaitTask
            
            return QueryResponse(user.Value, stocks, options)
        }
                
    interface IApplicationService
    
    member _.Handle (cmd:SendEmail) = task {
        do! cmd.input |> email.SendWithInput
        return Ok
    }
    
    member _.Handle sendWelcome = task {
        let! user = sendWelcome.userId |> storage.GetUser 
        match user with
        | Some user ->
            do! email.SendWithTemplate
                    (Recipient(email=user.State.Email, name=user.State.Name))
                    Sender.Support
                    EmailTemplate.NewUserWelcome
                    (System.Object())
                    
            return Ok ()
            
        | None -> return "User not found" |> Error
        
    }
            
    member _.Handle (_:Query) =
        task {
            let! users = storage.GetUserEmailIdPairs()
            
            let! result = 
                users
                |> Seq.map (fun emailId -> emailId.Id |> buildQueryResponse)
                |> Async.Parallel
                |> Async.StartAsTask
                
            return result
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
