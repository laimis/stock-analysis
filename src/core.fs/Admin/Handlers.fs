namespace core.fs.Admin

open core.Notes
open core.Options
open core.Stocks
open core.fs.Services
open core.fs.Shared
open core.fs.Shared.Adapters.CSV
open core.fs.Shared.Adapters.Email
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts

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
            let! notes = portfolio.GetNotes(userId) |> Async.AwaitTask
            let! stocks = portfolio.GetStocks(userId) |> Async.AwaitTask
            
            return QueryResponse(user.Value, stocks, options, notes)
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
                    
            return ServiceResponse.Ok
            
        | None -> return ResponseUtils.failed "User not found"
        
    }
            
    member _.Handle (_:Query) =
        task {
            let! users = storage.GetUserEmailIdPairs()
            
            let! result = 
                users
                |> Seq.map (fun emailId -> emailId.Id |> buildQueryResponse)
                |> Async.Parallel
                |> Async.StartAsTask
                
            return ServiceResponse<QueryResponse seq>(result)
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
        
        return ExportResponse(filename, CSVExport.users csvWriter users) |> ResponseUtils.success<ExportResponse>
    }