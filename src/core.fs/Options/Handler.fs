namespace core.fs.Options

open System
open System.ComponentModel.DataAnnotations
open core.Options
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.CSV
open core.fs.Adapters.Storage
open core.fs.Services

type OptionType =
    | Call
    | Put
    
    with
        override this.ToString() =
            match this with
            | Call -> nameof Call
            | Put -> nameof Put
            
        member this.ToEnum() =
            match this with
            | Call -> core.Options.OptionType.CALL
            | Put -> core.Options.OptionType.PUT
            
        static member FromString(value:string) =
            match value with
            | nameof Call -> Call
            | nameof Put -> Put
            | "CALL" -> Call
            | "PUT" -> Put
            | _ -> failwithf $"Invalid option type: %s{value}"
            
type OptionTransaction =
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
    | Buy of OptionTransaction * UserId
    | Sell of OptionTransaction * UserId

type DetailsQuery = { OptionId: Guid; UserId: UserId }
type ChainQuery = { Ticker: Ticker; UserId: UserId }
type OwnershipQuery = { UserId: UserId; Ticker: Ticker }

type Handler(
    accounts: IAccountStorage,
    brokerage: IBrokerage,
    stockInfo: IStockInfoProvider,
    storage: IPortfolioStorage,
    csvWriter: ICSVWriter) =

    interface IApplicationService

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

                let openOptionsTasks =
                    options
                    |> Seq.filter (fun o -> o.State.Closed.HasValue |> not)
                    |> Seq.map _.State
                    |> Seq.sortBy (fun o -> o.Ticker.Value, o.Expiration)
                    |> Seq.map (fun o ->
                        task {
                            let! chain = stockInfo.GetOptions user.State o.Ticker (Some o.Expiration) None None

                            let detail =
                                match chain with
                                | Ok chain -> chain.FindMatchingOption(o.StrikePrice, o.ExpirationDate, o.OptionType)
                                | Error _ -> None

                            return OwnedOptionView(o, detail)
                        })

                let! openOptions = System.Threading.Tasks.Task.WhenAll(openOptionsTasks)

                let! brokerageAccount = brokerage.GetAccount(user.State)

                let brokeragePositions, brokerageOrders =
                    brokerageAccount
                    |> Result.map (fun a ->
                        a.OptionPositions |> Seq.filter (fun p ->
                            openOptions
                            |> Seq.exists (fun o ->
                                o.Ticker.Value = p.Ticker.Value.Value
                                && o.StrikePrice = p.StrikePrice
                                && o.OptionType = p.OptionType)
                            |> not), a.Orders)
                    |> Result.defaultValue (Seq.empty, Array.empty)

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
            let buy (opt: OwnedOption) (data: OptionTransaction) =
                opt.Buy(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)

            let sell (opt: OwnedOption) (data: OptionTransaction) =
                opt.Sell(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)

            let data, userId, func =
                match cmd with
                | Buy (buyData, userId) -> (buyData, userId, buy)
                | Sell (sellData, userId) -> (sellData, userId, sell)

            let! user = accounts.GetUser(userId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | _ ->

                let optionType = data.OptionType.ToEnum()

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
                let! chain = stockInfo.GetOptions user.State option.State.Ticker None None None
                
                return
                    chain |> Result.map (fun c ->
                        let detail = c.FindMatchingOption(
                            strikePrice = option.State.StrikePrice,
                            expirationDate = option.State.ExpirationDate,
                            optionType = option.State.OptionType
                        )
                        OwnedOptionView(option.State, optionDetail = detail)
                    )
        }

    member this.Handle(query: ChainQuery) =
        task {
            let! user = accounts.GetUser(userId = query.UserId)

            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->

                let! priceResult = stockInfo.GetQuote user.State query.Ticker

                let price =
                    match priceResult with
                    | Ok price -> Some price.Price
                    | Error _ -> None

                let! details = stockInfo.GetOptions user.State query.Ticker None None None
                
                return details |> Result.map (fun d -> OptionDetailsViewModel(price, d))
        }
