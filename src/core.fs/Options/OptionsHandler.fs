namespace core.fs.Options

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.CSV
open core.fs.Adapters.Logging
open core.fs.Adapters.Options
open core.fs.Adapters.Storage
open core.fs.Services
            
type OptionTransactionInput =
    {
        [<Range(1, 10000)>]
        [<Required>]
        StrikePrice:Nullable<decimal>
        [<Required>]
        ExpirationDate : Nullable<DateTimeOffset>
        [<Required>]
        OptionType : OptionType        
        [<Range(1, 10000, ErrorMessage = "Invalid number of contracts specified")>]
        NumberOfContracts : int
        [<Range(1, 100000)>]
        [<Required>]
        Premium : Nullable<decimal>
        [<Required>]
        Filled : Nullable<DateTimeOffset>
        Notes : string
        [<Required>]
        Ticker : Ticker
    }
    
type DashboardQuery = { UserId:UserId }
type ExportQuery = { UserId: UserId }
type ExpireData = { PositionId: OptionPositionId; UserId: UserId }
type ExpireCommand =
    | Expire of ExpireData
    | Assign of ExpireData

type LookupData = { Ticker: Ticker; StrikePrice: decimal; Expiration: DateTimeOffset; UserId: UserId }

type ExpireViaLookupCommand =
    | ExpireViaLookup of LookupData
    | AssignViaLookup of LookupData

type DeleteOptionPositionCommand = { PositionId: OptionPositionId; UserId: UserId }

type OptionPositionQuery = { PositionId: OptionPositionId; UserId: UserId }
type RemoveOptionPositionLabelCommand = { Key: string; PositionId: OptionPositionId; UserId: UserId }
type SetOptionPositionLabel = { Key: string; Value: string; PositionId: OptionPositionId; }
type ChainQuery = { Ticker: Ticker; UserId: UserId }
type OptionOwnershipQuery = { UserId: UserId; Ticker: Ticker }
type OptionPricingQuery = { UserId: UserId; Symbol: OptionTicker}

type OptionContractInput = {
    StrikePrice: decimal
    OptionType: OptionType
    ExpirationDate: OptionExpiration
    Filled: DateTimeOffset
    Quantity: int
    Cost: decimal
}
type OpenOptionPositionCommand = {
    [<Required>]
    UnderlyingTicker: Ticker option
    [<Required>]
    Contracts: OptionContractInput[]
    [<Required>]
    Filled: DateTimeOffset option
    [<Required>]
    Notes: string option
    [<Required>]
    Strategy: string option
}
type CreatePendingOptionPositionCommand = {
    [<Required>]
    UnderlyingTicker: Ticker option
    [<Required>]
    Contracts: OptionContractInput[]
    [<Required>]
    Notes: string option
    [<Required>]
    Strategy: string option
    [<Required>]
    Cost: decimal option
}

type CloseContractsCommand = {
    PositionId: OptionPositionId
    UserId: UserId
    Contracts: OptionContractInput[]
}

type OpenContractsCommand = {
    PositionId: OptionPositionId
    UserId: UserId
    Contracts: OptionContractInput[]
}

type AddOptionNotesCommand = {
    PositionId: OptionPositionId
    UserId: UserId
    Notes: string
}

type CloseOptionPositionCommand = {
    [<Required>]
    PositionId: OptionPositionId
    [<Required>]
    Notes:string option
}

type OptionsHandler(accounts: IAccountStorage, brokerage: IBrokerage, storage: IPortfolioStorage, csvWriter: ICSVWriter, logger:ILogger) =

    interface IApplicationService

    member this.Handle(userId:UserId, command:CreatePendingOptionPositionCommand) = task {
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            
            let pendingPosition =
                OptionPosition.``open`` command.UnderlyingTicker.Value DateTimeOffset.UtcNow
                |> OptionPosition.establishDesiredCost command.Cost.Value DateTimeOffset.UtcNow
            
            let pendingPositionWithOrders =
                command.Contracts
                |> Array.fold (fun position (contract:OptionContractInput) ->
                    match contract.Quantity with
                    | x when x > 0 -> position |> OptionPosition.createPendingBuyOrder contract.ExpirationDate contract.StrikePrice contract.OptionType x DateTimeOffset.UtcNow
                    | x when x < 0 -> position |> OptionPosition.createPendingSellOrder contract.ExpirationDate contract.StrikePrice contract.OptionType -x DateTimeOffset.UtcNow
                    | _ -> position
                    ) pendingPosition
                |> OptionPosition.addNotes command.Notes DateTimeOffset.UtcNow
                |> OptionPosition.setLabelIfValueNotNone "strategy" command.Strategy DateTimeOffset.UtcNow
            
            do! storage.SaveOptionPosition userId None pendingPositionWithOrders
            return OptionPositionView(pendingPosition, None) |> Ok
    }
    
    member this.Handle(userId:UserId, command: OpenOptionPositionCommand) = task {
        
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
        
            let openPosition = OptionPosition.``open`` command.UnderlyingTicker.Value command.Filled.Value
            
            let withAttribute =
                command.Contracts
                |> Array.fold (fun position (contract:OptionContractInput) ->
                    match contract.Quantity with
                    | x when x > 0 -> position |> OptionPosition.buyToOpen contract.ExpirationDate contract.StrikePrice contract.OptionType x contract.Cost contract.Filled
                    | x when x < 0 -> position |> OptionPosition.sellToOpen contract.ExpirationDate contract.StrikePrice contract.OptionType -x contract.Cost contract.Filled
                    | _ -> position
                    ) openPosition
                |> OptionPosition.addNotes command.Notes DateTimeOffset.UtcNow
                |> OptionPosition.setLabelIfValueNotNone "strategy" command.Strategy DateTimeOffset.UtcNow
                
            match withAttribute.Transactions with
            | [] ->
                return "No transactions found" |> ServiceError |> Error
            | _ ->
                do! storage.SaveOptionPosition userId None withAttribute
                return OptionPositionView(withAttribute, None) |> Ok
    }
    
    member this.Handle(command: AddOptionNotesCommand) = task {
        let! user = command.UserId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! previous = storage.GetOptionPosition command.PositionId command.UserId
            match previous with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some o ->
                
                do!
                    o
                    |> OptionPosition.addNotes (Some command.Notes) DateTimeOffset.UtcNow
                    |> storage.SaveOptionPosition command.UserId previous
                    
                return true |> Ok
    }
    
    member this.Handle(command: CloseOptionPositionCommand, userId:UserId) = task {
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! openPosition = storage.GetOptionPosition command.PositionId userId
            match openPosition with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some op when op.IsClosed -> return "Option is already closed" |> ServiceError |> Error
            | Some op ->
                
                let withClosedPosition =
                    op
                    |> OptionPosition.close command.Notes DateTimeOffset.UtcNow
                
                do! storage.SaveOptionPosition userId openPosition withClosedPosition
                
                return OptionPositionView(withClosedPosition, None) |> Ok
    }
    
    member this.Handle(command: CloseContractsCommand) = task {
        let! user = command.UserId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! openPosition = storage.GetOptionPosition command.PositionId command.UserId
            match openPosition with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some op when op.IsClosed -> return "Option is closed, contracts can't be modified" |> ServiceError |> Error
            | Some op ->
                
                let withClosedContracts =
                    command.Contracts
                    |> Array.fold (fun position (contract:OptionContractInput) ->
                        match contract.Quantity with
                        | x when x > 0 -> position |> OptionPosition.buyToClose contract.ExpirationDate contract.StrikePrice contract.OptionType x contract.Cost contract.Filled
                        | x when x < 0 -> position |> OptionPosition.sellToClose contract.ExpirationDate contract.StrikePrice contract.OptionType -x contract.Cost contract.Filled
                        | _ -> position
                        ) op
                
                do! storage.SaveOptionPosition command.UserId openPosition withClosedContracts
                
                return OptionPositionView(withClosedContracts, None) |> Ok 
    }
    
    member this.Handle(command: OpenContractsCommand) = task {
        let! user = command.UserId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! openPosition = storage.GetOptionPosition command.PositionId command.UserId
            match openPosition with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some op when op.IsClosed -> return "Option is closed, contracts can't be modified" |> ServiceError |> Error
            | Some op ->
                
                let withOpenedContracts =
                    command.Contracts
                    |> Array.fold (fun position (contract:OptionContractInput) ->
                        match contract.Quantity with
                        | x when x > 0 -> position |> OptionPosition.buyToOpen contract.ExpirationDate contract.StrikePrice contract.OptionType x contract.Cost contract.Filled
                        | x when x < 0 -> position |> OptionPosition.sellToOpen contract.ExpirationDate contract.StrikePrice contract.OptionType -x contract.Cost contract.Filled
                        | _ -> position
                        ) op
                
                do! storage.SaveOptionPosition command.UserId openPosition withOpenedContracts
                
                return OptionPositionView(withOpenedContracts, None) |> Ok
    }
    
    member this.Handle(command: RemoveOptionPositionLabelCommand) = task {
        let! user = command.UserId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! option = storage.GetOptionPosition command.PositionId command.UserId
            match option with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some option ->
                
                do!
                    option
                    |> OptionPosition.deleteLabel command.Key DateTimeOffset.UtcNow
                    |> storage.SaveOptionPosition command.UserId (Some option)
                    
                return true |> Ok
    }
    
    member this.Handle(userId: UserId, command: SetOptionPositionLabel) = task {
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! option = storage.GetOptionPosition command.PositionId userId
            match option with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some option ->
                
                do!
                    option
                    |> OptionPosition.setLabel command.Key command.Value DateTimeOffset.UtcNow
                    |> storage.SaveOptionPosition userId (Some option)
                    
                return true |> Ok
    }
        
    member this.Handle(request: DashboardQuery) =
        task {
            let! user = request.UserId |> accounts.GetUser
            
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                
                let userId = user.Id |> UserId
                let! optionPositions = storage.GetOptionPositions userId
                
                let closedOptions =
                    optionPositions
                    |> Seq.filter _.IsClosed
                    |> Seq.sortByDescending _.Closed.Value
                    |> Seq.map (fun o -> OptionPositionView(o, None))

                let chainLookupMap = Dictionary<Ticker, OptionChain>()
                
                let! openAndPendingOptions =
                    optionPositions
                    |> Seq.filter (fun o -> o.IsClosed |> not)
                    |> Seq.map (fun o ->
                        async {
                            match chainLookupMap.TryGetValue(o.UnderlyingTicker) with
                            | true, chain -> return OptionPositionView(o, chain |> Some)
                            | _ ->
                            let! chain = brokerage.GetOptionChain user.State o.UnderlyingTicker |> Async.AwaitTask

                            let chain =
                                match chain with
                                | Error _ -> None
                                | Ok chain ->
                                    chainLookupMap.Add(o.UnderlyingTicker, chain)
                                    chain |> Some

                            return OptionPositionView(o, chain)
                        })
                    |> Async.Sequential
                
                let! brokerageAccount = brokerage.GetAccount(user.State)

                let brokeragePositions, brokerageOrders =
                    brokerageAccount
                    |> Result.map (fun a ->
                        
                        let positions =
                            a.OptionPositions |> Seq.filter (fun p ->
                                openAndPendingOptions
                                |> Seq.exists (fun o -> o.UnderlyingTicker = p.Ticker && o.IsOpen)
                                |> not)
                            
                        let orders = a.OptionOrders
                                
                        positions, orders
                    )
                    |> Result.defaultValue (Seq.empty, Array.empty)
                
                let! _ =
                    brokerageOrders
                    |> Seq.filter _.IsActive
                    |> Seq.map (fun o -> o.Contracts)
                    |> Seq.concat
                    |> Seq.map(fun l -> async {
                        if chainLookupMap.ContainsKey(l.UnderlyingTicker) |> not then
                            let! chain = brokerage.GetOptionChain user.State l.UnderlyingTicker |> Async.AwaitTask
                            match chain with
                            | Ok chain -> chainLookupMap.Add(l.UnderlyingTicker, chain)
                            | Error _ -> ()
                    })
                    |> Async.Sequential
                    
                let brokerageOrderViews =
                    brokerageOrders
                    |> Seq.map (fun o ->
                        let ticker = o.Contracts[0].UnderlyingTicker
                        let chain =
                            match chainLookupMap.TryGetValue(ticker) with
                            | true, chain -> chain |> Some
                            | _ -> None
                        OptionOrderView(o, chain)
                    )
                    
                let openOptions = openAndPendingOptions |> Seq.filter _.IsOpen
                let pendingOptions = openAndPendingOptions |> Seq.filter _.IsPending

                return OptionDashboardView(pendingOptions, openOptions, closedOptions, brokeragePositions, brokerageOrderViews) |> Ok
        }
        
    member _.Handle(ownership:OptionOwnershipQuery) : System.Threading.Tasks.Task<Result<OptionPositionView seq, ServiceError>> = task {
        let! options =
            ownership.UserId
            |> storage.GetOptionPositions
        
        let filteredOptions =
            options
            |> Seq.filter (fun o -> o.UnderlyingTicker = ownership.Ticker && o.IsOpen)
            |> Seq.map (fun o -> OptionPositionView(o, None))
            
        return filteredOptions |> Ok
    }

    member _.Handle(request: ExportQuery) = task {
        let! options = request.UserId |> storage.GetOptionPositions

        let csv = options |> CSVExport.options csvWriter

        return ExportResponse("options" |> CSVExport.generateFilename, csv)
    }

    member this.Handle(command: ExpireCommand) =
        task {
            let data, _ =
                match command with
                | Expire data -> (data, false)
                | Assign data -> (data, true)

            let! option = storage.GetOptionPosition data.PositionId data.UserId

            match option with
            | None ->
                return
                    $"option for id {data.PositionId} not found"
                    |> ServiceError |> Error
            | Some _ ->
                // fail, need to implement
                return "Not implemented" |> ServiceError |> Error
        }

    member this.Handle(_: ExpireViaLookupCommand) =
        "Not implemented" |> ServiceError |> Error

    member this.Handle(command: DeleteOptionPositionCommand) =
        task {
            let! opt = storage.GetOptionPosition command.PositionId command.UserId

            match opt with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some opt ->
                do! opt |> OptionPosition.delete |> storage.DeleteOptionPosition command.UserId (Some opt)
                return true |> Ok
        }

    member this.Handle(query: OptionPositionQuery) =
        task {
            let! user = accounts.GetUser(query.UserId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->

                let! option = storage.GetOptionPosition query.PositionId query.UserId
                match option with
                | None -> return "Option not found" |> ServiceError |> Error
                | Some option ->
                    let! chain = brokerage.GetOptionChain user.State option.UnderlyingTicker
                    return
                        match chain with
                        | Error e ->
                            logger.LogError($"Unable to get option chain for position {query.PositionId}, {option.UnderlyingTicker}: {e}")
                            OptionPositionView(option, None) |> Ok
                        | Ok chain ->
                            OptionPositionView(option, chain |> Some) |> Ok
        }

    member this.Handle(query: OptionPricingQuery) =
        task {
            let! user = accounts.GetUser(query.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some _ ->
                
                let! optionPricings = accounts.GetOptionPricing query.UserId query.Symbol
                return optionPricings |> Ok
        }
    member this.Handle(query: ChainQuery) =
        task {
            let! user = accounts.GetUser(userId = query.UserId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->

                let! details = brokerage.GetOptionChain user.State query.Ticker
                
                return details |> Result.map OptionChainView
        }
