namespace core.fs.Stocks.Lists

open System
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Adapters.Storage
open core.fs.Services


type GetLists =
    {
        UserId:UserId
    }

type GetList =
    {
        Id: Guid
        UserId: UserId
    }
    
type ExportList =
    {
        Id: Guid
        UserId: UserId
        JustTickers: bool
    }
    
type AddStockToList =
    {
        [<Required>]
        Id: Guid
        [<Required>]
        Ticker: Ticker
    }
    
type RemoveStockFromList =
    {
        [<Required>]
        Id: Guid
        UserId: UserId
        [<Required>]
        Ticker: Ticker
    }
    
type AddTagToList =
    {
        [<Required>]
        Id: Guid
        UserId: UserId
        [<Required>]
        [<MinLength(1)>]
        [<MaxLength(50)>]
        Tag: string
    }
    
type RemoveTagFromList =
    {
        [<Required>]
        Tag: string
        [<Required>]
        Id: Guid
        UserId: UserId
    }
    
type Create =
    {
        Name: string
        Description: string
    }
    
type Update =
    {
        [<Required>]
        Id: Guid
        [<Required>]
        Name: string
        Description: string
    }
    
type Delete =
    {
        Id: Guid
        UserId: UserId
    }
    
type Clear =
    {
        Id: Guid
        UserId: UserId
    }
    
type Handler(accounts:IAccountStorage, portfolio:IPortfolioStorage, csvWriter:ICSVWriter) =
    interface IApplicationService
    
    member _.Handle (command: GetLists) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! lists = portfolio.GetStockLists(command.UserId)
            let states = lists |> Seq.map _.State |> Seq.sortBy _.Name |> Seq.toArray
            return states |> Ok
    }
    
    member _.Handle (command: GetList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found"  |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id command.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ -> return list.State |> Ok
    }
    
    member _.Handle (command: ExportList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id command.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                let filename = CSVExport.generateFilename($"Stocks_{list.State.Name}");
                let response = ExportResponse(filename, CSVExport.stockList csvWriter list.State command.JustTickers)
                return response |> Ok
    }
    
    member _.HandleAddStockToList userId (command: AddStockToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id userId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                list.AddStock(ticker=command.Ticker, note=null)
                do! portfolio.SaveStockList list userId
                return list.State |> Ok
    }
    
    member _.Handle (command: RemoveStockFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id command.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                list.RemoveStock(ticker=command.Ticker)
                do! portfolio.SaveStockList list command.UserId
                return list.State |> Ok
    }
    
    member _.HandleAddTagToList userId (command: AddTagToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id userId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                list.AddTag(tag=command.Tag)
                do! portfolio.SaveStockList list userId
                return list.State |> Ok
    }
    
    member _.Handle (command: RemoveTagFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id command.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                list.RemoveTag(tag=command.Tag)
                do! portfolio.SaveStockList list command.UserId
                return list.State |> Ok
    }
    
    member _.HandleCreate userId (command: Create) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! lists = portfolio.GetStockLists userId
            let exists = lists |> Seq.exists (fun x -> x.State.Name = command.Name)
            match exists with
            | false ->
                let newList = StockList(name=command.Name, description=command.Description, userId=(userId |> IdentifierHelper.getUserId))
                do! portfolio.SaveStockList newList userId
                return newList.State |> Ok
            | true ->
                return "List already exists" |> ServiceError |> Error
    }
    
    member _.HandleUpdate (command: Update) userId = task {
        System.Console.WriteLine("Handling update")
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id userId
            match list with
            | null ->
                System.Console.WriteLine("List not found")
                return "List not found" |> ServiceError |> Error
            | _ ->
                System.Console.WriteLine("Updating list")
                list.Update(name=command.Name, description=command.Description)
                do! portfolio.SaveStockList list userId
                return list.State |> Ok
    }
    
    member _.Handle (command: Delete) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList command.Id command.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                do! portfolio.DeleteStockList list command.UserId
                return Ok ()
    }
    
    member _.Handle (clear:Clear) = task {
        let! user = accounts.GetUser(clear.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = portfolio.GetStockList clear.Id clear.UserId
            match list with
            | null -> return "List not found" |> ServiceError |> Error
            | _ ->
                list.Clear()
                do! portfolio.SaveStockList list clear.UserId
                return Ok ()
    }
