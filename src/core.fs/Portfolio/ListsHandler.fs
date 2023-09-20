namespace core.fs.Portfolio.Lists

open System
open System.ComponentModel.DataAnnotations
open core.Portfolio
open core.Shared
open core.Shared.Adapters.CSV
open core.Shared.Adapters.Storage
open core.fs


type GetLists =
    {
        UserId:Guid
    }

type GetList =
    {
        Name: string
        UserId: Guid
    }
    
type ExportList =
    {
        Name: string
        UserId: Guid
        JustTickers: bool
    }
    
type AddStockToList =
    {
        [<Required>]
        Name: string
        UserId: Guid
        [<Required>]
        Ticker: Ticker
    }
    static member WithUserId (userId:Guid) (command:AddStockToList) = { command with UserId = userId }
    
type RemoveStockFromList =
    {
        [<Required>]
        Name: string
        UserId: Guid
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
        UserId: Guid
    }
    static member WithUserId (userId:Guid) (command:AddTagToList) = { command with UserId = userId }
    
type RemoveTagFromList =
    {
        [<Required>]
        Tag: string
        [<Required>]
        Name: string
        UserId: Guid
    }
    
type Create =
    {
        Name: string
        Description: string
        UserId: Guid
    }
    static member WithUserId (userId:Guid) (command:Create) = { command with UserId = userId }
    
type Update =
    {
        Name: string
        Description: string
        UserId: Guid
    }
    static member WithUserId (userId:Guid) (command:Update) = { command with UserId = userId }
    
type Delete =
    {
        Name: string
        UserId: Guid
    }
    
type Handler(accounts:IAccountStorage, portfolio:IPortfolioStorage, csvWriter:ICSVWriter) =
    interface IApplicationService
    
    member _.Handle (command: GetLists) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState array>
        | _ ->
            let! lists = portfolio.GetStockLists(command.UserId)
            let states = lists |> Seq.map (fun l -> l.State) |> Seq.sortBy (fun s -> s.Name) |> Seq.toArray
            return states |> ResponseUtils.success
    }
    
    member _.Handle (command: GetList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ -> return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: ExportList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<ExportResponse>
            | _ ->
                let filename = CSVExport.GenerateFilename($"Stocks_{command.Name}");
                let response = ExportResponse(filename, CSVExport.Generate(csvWriter, list.State, command.JustTickers))
                return response |> ResponseUtils.success
    }
    
    member _.Handle (command: AddStockToList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.AddStock(ticker=command.Ticker, note=null)
                do! portfolio.Save(list, userId=command.UserId)
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: RemoveStockFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.RemoveStock(ticker=command.Ticker)
                do! portfolio.Save(list, userId=command.UserId)
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: AddTagToList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.AddTag(tag=command.Tag)
                do! portfolio.Save(list, userId=command.UserId)
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: RemoveTagFromList) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.RemoveTag(tag=command.Tag)
                do! portfolio.Save(list, userId=command.UserId)
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: Create) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null ->
                let newList = StockList(name=command.Name, description=command.Description, userId=command.UserId)
                do! portfolio.Save(newList, userId=command.UserId)
                return newList.State |> ResponseUtils.success
            | _ ->
                return "List already exists" |> ResponseUtils.failedTyped<StockListState>
    }
    
    member _.Handle (command: Update) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<StockListState>
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failedTyped<StockListState>
            | _ ->
                list.Update(name=command.Name, description=command.Description)
                do! portfolio.Save(list, userId=command.UserId)
                return list.State |> ResponseUtils.success
    }
    
    member _.Handle (command: Delete) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! list = portfolio.GetStockList(name=command.Name, userId=command.UserId)
            match list with
            | null -> return "List not found" |> ResponseUtils.failed
            | _ ->
                do! portfolio.DeleteStockList(list, userId=command.UserId)
                return ServiceResponse()
    }