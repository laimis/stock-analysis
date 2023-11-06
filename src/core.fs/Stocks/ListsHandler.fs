namespace core.fs.Stocks.Lists

open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs.Services
open core.fs.Shared
open core.fs.Shared.Adapters.CSV
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts


type GetLists =
    {
        UserId:UserId
    }

type GetList =
    {
        Name: string
        UserId: UserId
    }
    
type ExportList =
    {
        Name: string
        UserId: UserId
        JustTickers: bool
    }
    
type AddStockToList =
    {
        [<Required>]
        Name: string
        [<Required>]
        Ticker: Ticker
    }
    
type RemoveStockFromList =
    {
        [<Required>]
        Name: string
        UserId: UserId
        [<Required>]
        Ticker: Ticker
    }
    
type AddTagToList =
    {
        [<Required>]
        [<MinLength(1)>]
        [<MaxLength(50)>]
        Tag: string
        [<Required>]
        Name: string
    }
    
type RemoveTagFromList =
    {
        [<Required>]
        Tag: string
        [<Required>]
        Name: string
        UserId: UserId
    }
    
type Create =
    {
        Name: string
        Description: string
    }
    
type Update =
    {
        Name: string
        Description: string
    }
    
type Delete =
    {
        Name: string
        UserId: UserId
    }
    
type Clear =
    {
        Name: string
        UserId: UserId
    }
    
type Handler(accounts:IAccountStorage, portfolio:IPortfolioStorage, csvWriter:ICSVWriter) =
    interface IApplicationService
    
    member _.Handle (command: GetLists) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState array>
        | _ ->
            let! lists = portfolio.GetStockLists(command.UserId)
            let states = lists |> Seq.map (fun l -> l.State) |> Seq.sortBy (fun s -> s.Name) |> Seq.toArray
            return states |> ResponseUtils.success
    }
    
    member _.Handle (command: GetList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name command.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ -> return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: ExportList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
        | _ ->
            let! list = portfolio.GetStockList command.Name command.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<ExportResponse>
            | _ ->
                let filename = CSVExport.generateFilename($"Stocks_{command.Name}");
                let response = ExportResponse(filename, CSVExport.stockList csvWriter list.State command.JustTickers)
                return response |> ResponseUtils.success
    }
    
    member _.HandleAddStockToList userId (command: AddStockToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name userId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.AddStock(ticker=command.Ticker, note=null)
                do! portfolio.SaveStockList list userId
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: RemoveStockFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name command.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.RemoveStock(ticker=command.Ticker)
                do! portfolio.SaveStockList list command.UserId
                return list.State |> ResponseUtils.success
    }
    
    member _.HandleAddTagToList userId (command: AddTagToList) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name userId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.AddTag(tag=command.Tag)
                do! portfolio.SaveStockList list userId
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: RemoveTagFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name command.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.RemoveTag(tag=command.Tag)
                do! portfolio.SaveStockList list command.UserId
                return list.State |> ResponseUtils.success
    }
    
    member _.HandleCreate userId (command: Create) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name userId
            match list with
            | null ->
                let newList = StockList(name=command.Name, description=command.Description, userId=(userId |> IdentifierHelper.getUserId))
                do! portfolio.SaveStockList newList userId
                return newList.State |> ResponseUtils.success
            | _ ->
                return "List already exists" |> ResponseUtils.failedTyped<StockListState>
    }
    
    member _.HandleUpdate userId (command: Update) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList command.Name userId
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.Update(name=command.Name, description=command.Description)
                do! portfolio.SaveStockList list userId
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: Delete) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! list = portfolio.GetStockList command.Name command.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failed
            | _ ->
                do! portfolio.DeleteStockList list command.UserId
                return Ok
    }
    
    member _.Handle (clear:Clear) = task {
        let! user = accounts.GetUser(clear.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! list = portfolio.GetStockList clear.Name clear.UserId
            match list with
            | null -> return "List not found" |> ResponseUtils.failed
            | _ ->
                list.Clear()
                do! portfolio.SaveStockList list clear.UserId
                return Ok
    }