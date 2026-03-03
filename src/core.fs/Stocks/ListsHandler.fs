namespace core.fs.Stocks.Lists

open System
open System.ComponentModel.DataAnnotations
open core.Shared
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
    
type Handler(accounts: IAccountStorage, stockLists: IStockListStorage, csvWriter: ICSVWriter) =
    interface IApplicationService
    
    member _.Handle (command: GetLists) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! lists = stockLists.GetStockLists command.UserId
            return lists |> Seq.sortBy _.Name |> Seq.toArray |> Ok
    }
    
    member _.Handle (command: GetList) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = stockLists.GetStockList command.Id command.UserId
            match list with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.Handle (command: ExportList) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! list = stockLists.GetStockList command.Id command.UserId
            match list with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l ->
                let filename = CSVExport.generateFilename $"Stocks_{l.Name}"
                let response = ExportResponse(filename, CSVExport.stockList csvWriter l command.JustTickers)
                return response |> Ok
    }
    
    member _.HandleAddStockToList userId (command: AddStockToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.AddTickerToStockList command.Id command.Ticker null userId
            match result with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.Handle (command: RemoveStockFromList) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.RemoveTickerFromStockList command.Id command.Ticker command.UserId
            match result with
            | None -> return "Ticker or list not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.HandleAddTagToList userId (command: AddTagToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.AddTagToStockList command.Id command.Tag userId
            match result with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.Handle (command: RemoveTagFromList) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.RemoveTagFromStockList command.Id command.Tag command.UserId
            match result with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.HandleCreate userId (command: Create) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! lists = stockLists.GetStockLists userId
            let exists = lists |> Seq.exists (fun x -> x.Name = command.Name)
            match exists with
            | true -> return "List already exists" |> ServiceError |> Error
            | false ->
                let! newList = stockLists.CreateStockList command.Name command.Description userId
                return newList |> Ok
    }
    
    member _.HandleUpdate (command: Update) userId = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.UpdateStockList command.Id command.Name command.Description userId
            match result with
            | None -> return "List not found" |> ServiceError |> Error
            | Some l -> return l |> Ok
    }
    
    member _.Handle (command: Delete) = task {
        let! user = accounts.GetUser command.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            do! stockLists.DeleteStockList command.Id command.UserId
            return Ok ()
    }
    
    member _.Handle (clear: Clear) = task {
        let! user = accounts.GetUser clear.UserId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! result = stockLists.ClearStockListTickers clear.Id clear.UserId
            match result with
            | None -> return "List not found" |> ServiceError |> Error
            | Some _ -> return Ok ()
    }