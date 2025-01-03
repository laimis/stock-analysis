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

type DeleteCommand = { OptionId: Guid; UserId: UserId }

type BuyOrSellCommand =
    | Buy of OptionTransactionInput * UserId
    | Sell of OptionTransactionInput * UserId

type DetailsQuery = { OptionId: Guid; UserId: UserId }
type ChainQuery = { Ticker: Ticker; UserId: UserId }
type OwnershipQuery = { UserId: UserId; Ticker: Ticker }

type OptionLegInput = {
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
    Legs: OptionLegInput[]
    [<Required>]
    Filled: DateTimeOffset option
}

type OptionsHandler(accounts: IAccountStorage, brokerage: IBrokerage, storage: IPortfolioStorage, csvWriter: ICSVWriter, logger:ILogger) =

    let fsOptionTypeConvert oldType =
        match oldType with
        | OptionType.CALL -> core.fs.Options.OptionType.Call 
        | OptionType.PUT -> core.fs.Options.OptionType.Put
        | _ -> raise (ArgumentException("Invalid option type"))
        
    interface IApplicationService

    member this.Handle(userId:UserId, command: OpenOptionPositionCommand) = task {
        Console.WriteLine("Opening option position")
        
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
        
            Console.WriteLine("Inside open, let's go!")
            
            let openPosition = OptionPosition.``open`` command.UnderlyingTicker.Value DateTimeOffset.UtcNow
            
            let withLegs =
                command.Legs
                |> Array.fold (fun position (leg:OptionLegInput) ->
                    match leg.Quantity with
                    | x when x > 0 -> position |> OptionPosition.buyToOpen leg.ExpirationDate leg.StrikePrice leg.OptionType x leg.Cost leg.Filled
                    | x when x < 0 -> position |> OptionPosition.sellToOpen leg.ExpirationDate leg.StrikePrice leg.OptionType -x leg.Cost leg.Filled
                    | _ -> position
                    ) openPosition
                
            match withLegs.Transactions with
            | [] ->
                return "No transactions found" |> ServiceError |> Error
            | _ ->
                do! storage.SaveOptionPosition userId None withLegs
                return withLegs |> Ok
    }
        
    member this.Handle(request: DashboardQuery) =
        task {
            let! user = request.UserId |> accounts.GetUser
            
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                let! options = storage.GetOwnedOptions(UserId user.Id)

                let closedOptions =
                    options
                    |> Seq.filter (fun o -> o.State.Closed.HasValue)
                    |> Seq.map (fun o -> o.State)
                    |> Seq.sortByDescending (fun o -> o.FirstFill.Value)
                    |> Seq.map (fun o -> OwnedOptionView(o, None))

                let! openOptions =
                    options
                    |> Seq.filter (fun o -> o.State.Closed.HasValue |> not)
                    |> Seq.map _.State
                    |> Seq.sortBy (fun o -> o.Ticker.Value, o.Expiration)
                    |> Seq.map (fun o ->
                        async {
                            let! chain = brokerage.GetOptionChain user.State o.Ticker |> Async.AwaitTask

                            let fsOptionType = fsOptionTypeConvert o.OptionType
                                
                            let detail =
                                match chain with
                                | Ok chain -> chain.FindMatchingOption(o.StrikePrice, o.Expiration, fsOptionType)
                                | Error _ -> None

                            return OwnedOptionView(o, detail)
                        })
                    |> Async.Sequential

                let! brokerageAccount = brokerage.GetAccount(user.State)

                let brokeragePositions, brokerageOrders =
                    brokerageAccount
                    |> Result.map (fun a ->
                        
                        let positions =
                            a.OptionPositions |> Seq.filter (fun p ->
                                openOptions
                                |> Seq.exists (fun o ->
                                    o.Ticker.Value = p.Ticker.Value
                                    && o.StrikePrice = p.StrikePrice
                                    && o.OptionType = p.OptionType)
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
                            return chainLookupMap[l.UnderlyingTicker].FindMatchingOption(l.StrikePrice, l.ExpirationDate, l.OptionType)
                        else
                            let! chain = brokerage.GetOptionChain user.State l.UnderlyingTicker |> Async.AwaitTask
                            match chain with
                            | Ok chain ->
                                chainLookupMap.Add(l.UnderlyingTicker, chain)
                                return chain.FindMatchingOption(l.StrikePrice, l.ExpirationDate, l.OptionType)
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
                                    let found = matchingChainDetails |> Seq.tryFind (fun d -> d.StrikePrice = l.StrikePrice && d.ParsedExpirationDate |> Option.defaultValue DateTimeOffset.MinValue |> _.Date = l.ExpirationDate.Date && d.OptionType = l.OptionType)
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
        
    member _.Handle(ownership:OwnershipQuery) : System.Threading.Tasks.Task<Result<OwnedOptionState seq, ServiceError>> = task {
        let! options = ownership.UserId |> storage.GetOwnedOptions
        
        let filteredOptions =
            options
            |> Seq.filter (fun o -> o.State.Ticker = ownership.Ticker && o.State.Active)
            |> Seq.map _.State
            
        return filteredOptions |> Ok
    }

    member _.Handle(request: ExportQuery) =
        task {
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

    member this.Handle(command: DeleteCommand) =
        task {
            let! opt = storage.GetOwnedOption command.OptionId command.UserId

            match opt with
            | null -> return "Unable to find option do delete" |> ServiceError |> Error
            | _ ->
                opt.Delete()
                do! storage.SaveOwnedOption opt command.UserId
                return Ok ()
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

    member this.Handle(query: DetailsQuery) =
        task {
            let! user = accounts.GetUser(query.UserId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->

                let! option = storage.GetOwnedOption query.OptionId query.UserId
                let! chain = brokerage.GetOptionChain user.State option.State.Ticker
                
                let detail =
                        chain
                        |> Result.map (fun c ->
                            c.FindMatchingOption(
                                strikePrice = option.State.StrikePrice,
                                expirationDate = option.State.Expiration,
                                optionType = fsOptionTypeConvert option.State.OptionType
                            )
                        )
                        |> Result.defaultValue None
                        
                return OwnedOptionView(option.State, optionDetail = detail) |> Ok
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
