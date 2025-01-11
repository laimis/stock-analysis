namespace core.fs.Options

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Options
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
        OptionType : core.fs.Options.OptionType        
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
type ExpireData = { OptionId: Guid; UserId: UserId }
type ExpireCommand =
    | Expire of ExpireData
    | Assign of ExpireData

type LookupData = { Ticker: Ticker; StrikePrice: decimal; Expiration: DateTimeOffset; UserId: UserId }

type ExpireViaLookupCommand =
    | ExpireViaLookup of LookupData
    | AssignViaLookup of LookupData

type DeleteOptionPositionCommand = { PositionId: OptionPositionId; UserId: UserId }

type BuyOrSellCommand =
    | Buy of OptionTransactionInput * UserId
    | Sell of OptionTransactionInput * UserId

type OptionPositionQuery = { PositionId: OptionPositionId; UserId: UserId }
type RemoveOptionPositionLabelCommand = { Key: string; PositionId: OptionPositionId; UserId: UserId }
type SetOptionPositionLabel = { Key: string; Value: string; PositionId: OptionPositionId; }
type ChainQuery = { Ticker: Ticker; UserId: UserId }
type OptionOwnershipQuery = { UserId: UserId; Ticker: Ticker }

type OptionContractInput = {
    StrikePrice: decimal
    OptionType: core.fs.Options.OptionType
    ExpirationDate: string
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
    [<Required>]
    IsPaperPosition: bool option
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

type OptionsHandler(accounts: IAccountStorage, brokerage: IBrokerage, storage: IPortfolioStorage, csvWriter: ICSVWriter, logger:ILogger) =

    let fsOptionTypeConvert oldType =
        match oldType with
        | OptionType.CALL -> core.fs.Options.OptionType.Call 
        | OptionType.PUT -> core.fs.Options.OptionType.Put
        | _ -> raise (ArgumentException("Invalid option type"))
        
    interface IApplicationService

    member this.Handle(userId:UserId, command: OpenOptionPositionCommand) = task {
        
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
        
            let openPosition = OptionPosition.``open`` command.UnderlyingTicker.Value command.Filled.Value
            
            let withAttribute =
                command.Contracts
                |> Array.fold (fun position (leg:OptionContractInput) ->
                    match leg.Quantity with
                    | x when x > 0 -> position |> OptionPosition.buyToOpen leg.ExpirationDate leg.StrikePrice leg.OptionType x leg.Cost leg.Filled
                    | x when x < 0 -> position |> OptionPosition.sellToOpen leg.ExpirationDate leg.StrikePrice leg.OptionType -x leg.Cost leg.Filled
                    | _ -> position
                    ) openPosition
                |> OptionPosition.addNotes command.Notes DateTimeOffset.UtcNow
                |> OptionPosition.setLabelIfValueNotNone "strategy" command.Strategy DateTimeOffset.UtcNow
                |> OptionPosition.setLabelIfValueNotNone "isPaperPosition" (command.IsPaperPosition |> Option.map _.ToString()) DateTimeOffset.UtcNow
                
            match withAttribute.Transactions with
            | [] ->
                return "No transactions found" |> ServiceError |> Error
            | _ ->
                do! storage.SaveOptionPosition userId None withAttribute
                return OptionPositionView(withAttribute, None) |> Ok
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

                let! openOptions =
                    optionPositions
                    |> Seq.filter (fun o -> o.IsOpen)
                    |> Seq.map (fun o ->
                        async {
                            let! chain = brokerage.GetOptionChain user.State o.UnderlyingTicker |> Async.AwaitTask

                            let chain =
                                match chain with
                                | Error _ -> None
                                | Ok chain -> chain |> Some

                            return OptionPositionView(o, chain)
                        })
                    |> Async.Sequential

                let! brokerageAccount = brokerage.GetAccount(user.State)

                let brokeragePositions, brokerageOrders =
                    brokerageAccount
                    |> Result.map (fun a ->
                        
                        let positions =
                            a.OptionPositions |> Seq.filter (fun p ->
                                openOptions
                                |> Seq.exists (fun o -> o.UnderlyingTicker = p.Ticker)
                                |> not)
                            
                        let orders = a.OptionOrders
                                
                        positions, orders
                    )
                    |> Result.defaultValue (Seq.empty, Array.empty)
                
                let chainLookupMap = Dictionary<Ticker, OptionChain>()
                                 
                let! openBrokerageOrders =
                    brokerageOrders
                    |> Seq.filter _.IsActive
                    |> Seq.map (fun o -> o.Legs)
                    |> Seq.concat
                    |> Seq.map(fun l -> async {
                        // let's see if we can get the chain
                        if chainLookupMap.ContainsKey(l.UnderlyingTicker) then
                            return chainLookupMap[l.UnderlyingTicker].FindMatchingOption(l.StrikePrice, l.Expiration, l.OptionType)
                        else
                            let! chain = brokerage.GetOptionChain user.State l.UnderlyingTicker |> Async.AwaitTask
                            match chain with
                            | Ok chain ->
                                chainLookupMap.Add(l.UnderlyingTicker, chain)
                                return chain.FindMatchingOption(l.StrikePrice, l.Expiration, l.OptionType)
                            | Error _ -> return None
                    })
                    |> Async.Sequential
                    
                let matchingChainDetails =
                    openBrokerageOrders
                    |> Seq.choose id
                    
                let brokerageOrders =
                    brokerageOrders
                    |> Seq.map (fun o ->
                        match o.IsActive with
                        | true -> {
                                o with Legs = o.Legs |> Seq.map (fun l ->
                                    let found = matchingChainDetails |> Seq.tryFind (fun d -> d.StrikePrice = l.StrikePrice && d.Expiration = l.Expiration && d.OptionType = l.OptionType)
                                    match found with
                                    | Some d -> { l with Price = d.Mark |> Some }
                                    | None ->
                                        logger.LogError($"Unable to find matching option for {l.UnderlyingTicker} {l.StrikePrice} {l.OptionType} {l.Expiration}")
                                        l
                                    ) |> Seq.toArray
                            }
                        | false -> o
                    )

                return OptionDashboardView(closedOptions, openOptions, brokeragePositions, brokerageOrders) |> Ok
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
        let! options = request.UserId |> storage.GetOwnedOptions

        let csv = options |> CSVExport.options csvWriter

        return ExportResponse("options" |> CSVExport.generateFilename, csv)
    }

    member this.Handle(command: ExpireCommand) =
        task {
            let data, assign =
                match command with
                | Expire data -> (data, false)
                | Assign data -> (data, true)

            let! option = storage.GetOwnedOption data.OptionId data.UserId

            match option with
            | null ->
                return
                    $"option for id {data.OptionId} not found"
                    |> ServiceError |> Error
            | _ ->
                option.Expire(assign = assign)
                do! storage.SaveOwnedOption option data.UserId
                return option |> Ok
        }

    member this.Handle(command: ExpireViaLookupCommand) =
        task {
            let data, assigned =
                match command with
                | ExpireViaLookup data -> (data, false)
                | AssignViaLookup data -> (data, true)

            let! options = storage.GetOwnedOptions(data.UserId)

            let option =
                options
                |> Seq.tryFind (fun o ->
                    o.State.Ticker = data.Ticker
                    && o.State.StrikePrice = data.StrikePrice
                    && o.State.Expiration = data.Expiration)

            match option with
            | Some o ->
                o.Expire(assign = assigned)
                do! storage.SaveOwnedOption o data.UserId
                return o |> Ok

            | None ->
                return
                    $"option for ticker {data.Ticker} strike {data.StrikePrice} expiration {data.Expiration} not found"
                    |> ServiceError |> Error
        }

    member this.Handle(command: DeleteOptionPositionCommand) =
        task {
            let! opt = storage.GetOptionPosition command.PositionId command.UserId

            match opt with
            | None -> return "Option not found" |> ServiceError |> Error
            | Some opt ->
                do! opt |> OptionPosition.delete |> storage.DeleteOptionPosition command.UserId (Some opt)
                return true |> Ok
        }

    member this.Handle(cmd: BuyOrSellCommand) =
        task {
            let buy (opt: OwnedOption) (data: OptionTransactionInput) =
                opt.Buy(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)

            let sell (opt: OwnedOption) (data: OptionTransactionInput) =
                opt.Sell(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)

            let data, userId, func =
                match cmd with
                | Buy (buyData, userId) -> (buyData, userId, buy)
                | Sell (sellData, userId) -> (sellData, userId, sell)

            let! user = accounts.GetUser(userId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | _ ->

                let optionType =
                    match data.OptionType with
                    | Call -> OptionType.CALL
                    | Put -> OptionType.PUT
                    
                let! options = storage.GetOwnedOptions(userId)
                
                let option =
                    options
                    |> Seq.tryFind (fun o ->
                        o.IsMatch(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value))
                    |> Option.defaultWith (fun () ->
                        OwnedOption(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value, userId |> IdentifierHelper.getUserId))

                func option data

                do! storage.SaveOwnedOption option userId

                return option |> Ok
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

    member this.Handle(query: ChainQuery) =
        task {
            let! user = accounts.GetUser(userId = query.UserId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->

                let! details = brokerage.GetOptionChain user.State query.Ticker
                
                return details |> Result.map OptionChainView
        }
