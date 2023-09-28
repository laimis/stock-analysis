namespace core.fs.Options

open System
open core.Options
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.CSV
open core.fs.Shared
open core.fs.Shared.Adapters.Storage

type DashboardQuery(userId: Guid) =
    member _.UserId = userId

type ExportQuery(userId: Guid) =
    member _.UserId = userId
    member _.Filename = "options"

type ExpireData(optionId: Guid, userId: Guid) =
    member this.OptionId = optionId
    member this.UserId = userId

type ExpireCommand =
    | Expire of ExpireData
    | Assign of ExpireData

type ExpireViaLookupData(ticker: string, strikePrice: decimal, expiration: DateTimeOffset, userId: Guid) =
    member this.Ticker = ticker
    member this.StrikePrice = strikePrice
    member this.Expiration = expiration
    member this.UserId = userId

type ExpireViaLookupCommand =
    | ExpireViaLookup of ExpireViaLookupData
    | AssignViaLookup of ExpireViaLookupData

type DeleteCommand = { OptionId: Guid; UserId: Guid }

type BuyOrSellCommand =
    | Buy of OptionTransaction * Guid
    | Sell of OptionTransaction * Guid

type DetailsQuery = { OptionId: Guid; UserId: Guid }

type ChainQuery = { Ticker: string; UserId: Guid }

type Handler(accounts: IAccountStorage, brokerage: IBrokerage, storage: IPortfolioStorage, csvWriter: ICSVWriter) =

    interface IApplicationService

    member this.Handle(request: DashboardQuery) =
        task {
            let! user = request.UserId |> accounts.GetUser

            let! options = storage.GetOwnedOptions(user.Id)

            let closedOptions =
                options
                |> Seq.filter (fun o -> o.State.Closed.HasValue)
                |> Seq.map (fun o -> o.State)
                |> Seq.sortByDescending (fun o -> o.FirstFill.Value)
                |> Seq.map (fun o -> OwnedOptionView(o, null))

            let openOptionsTasks =
                options
                |> Seq.filter (fun o -> o.State.Closed.HasValue |> not)
                |> Seq.map (fun o -> o.State)
                |> Seq.sortBy (fun o -> o.Ticker, o.Expiration)
                |> Seq.map (fun o ->
                    task {
                        let! chain = brokerage.GetOptions(user.State, o.Ticker, o.Expiration)

                        let detail =
                            match chain.IsOk with
                            | true -> chain.Success.FindMatchingOption(o.StrikePrice, o.ExpirationDate, o.OptionType)
                            | false -> null

                        return OwnedOptionView(o, detail)
                    })

            let! openOptions = System.Threading.Tasks.Task.WhenAll(openOptionsTasks)

            let! brokerageAccount = brokerage.GetAccount(user.State)

            let brokeragePositions =
                match brokerageAccount.IsOk with
                | true ->
                    brokerageAccount.Success.OptionPositions
                    |> Seq.filter (fun p ->
                        openOptions
                        |> Seq.exists (fun o ->
                            o.Ticker = p.Ticker
                            && o.StrikePrice = p.StrikePrice
                            && o.OptionType = p.OptionType)
                        |> not)
                | false -> Seq.empty

            let brokerageOrders =
                match brokerageAccount.IsOk with
                | true -> brokerageAccount.Success.Orders
                | false -> Array.empty

            let view =
                OptionDashboardView(closedOptions, openOptions, brokeragePositions, brokerageOrders)

            return ServiceResponse<OptionDashboardView>(view)
        }

    member _.Handle(request: ExportQuery) =
        task {
            let! options = request.UserId |> storage.GetOwnedOptions

            let csv = CSVExport.Generate(csvWriter, options)

            return
                ExportResponse(request.Filename |> CSVExport.GenerateFilename, csv)
                |> ResponseUtils.success<ExportResponse>
        }

    member this.Handle(command: ExpireCommand) =
        task {
            let data, assign =
                match command with
                | Expire (data) -> (data, false)
                | Assign (data) -> (data, true)

            let! option = storage.GetOwnedOption data.OptionId data.UserId

            match option with
            | null ->
                return
                    $"option for id {data.OptionId} not found"
                    |> ResponseUtils.failedTyped<OwnedOption>
            | _ ->
                option.Expire(assign = assign)
                do! storage.SaveOwnedOption option data.UserId
                return ServiceResponse<OwnedOption>(option)
        }

    member this.Handle(command: ExpireViaLookupCommand) =
        task {
            let data, assigned =
                match command with
                | ExpireViaLookup (data) -> (data, false)
                | AssignViaLookup (data) -> (data, true)

            let! options = storage.GetOwnedOptions(data.UserId)

            let option =
                options
                |> Seq.tryFind (fun o ->
                    o.State.Ticker = data.Ticker
                    && o.State.StrikePrice = data.StrikePrice
                    && o.State.Expiration = data.Expiration)

            match option with
            | Some (o) ->
                o.Expire(assign = assigned)
                do! storage.SaveOwnedOption o data.UserId
                return ServiceResponse<OwnedOption>(o)

            | None ->
                return
                    $"option for ticker {data.Ticker} strike {data.StrikePrice} expiration {data.Expiration} not found"
                    |> ResponseUtils.failedTyped<OwnedOption>
        }

    member this.Handle(command: DeleteCommand) =
        task {
            let! opt = storage.GetOwnedOption command.OptionId command.UserId

            match opt with
            | null -> return ServiceResponse(ServiceError("Unable to find option do delete"))
            | _ ->
                opt.Delete()
                do! storage.SaveOwnedOption opt command.UserId
                return ServiceResponse()
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
            | null ->
                return
                    "User not found"
                    |> ResponseUtils.failedTyped<OwnedOption>
            | _ ->

                let optionType = Enum.Parse(typedefof<OptionType>, data.OptionType) :?> OptionType

                let! options = storage.GetOwnedOptions(userId)

                let option =
                    options
                    |> Seq.tryFind (fun o ->
                        o.IsMatch(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value))
                    |> Option.defaultWith (fun () ->
                        OwnedOption(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value, userId))

                func option data

                do! storage.SaveOwnedOption option userId

                return ServiceResponse<OwnedOption>(option)
        }

    member this.Handle(query: DetailsQuery) =
        task {
            let! user = accounts.GetUser(query.UserId)

            match user with
            | null ->
                return
                    "User not found"
                    |> ResponseUtils.failedTyped<OwnedOptionView>
            | _ ->

                let! option = storage.GetOwnedOption query.OptionId query.UserId
                let! chain = brokerage.GetOptions(user.State, option.State.Ticker)

                let detail =
                    chain.Success.FindMatchingOption(
                        strikePrice = option.State.StrikePrice,
                        expirationDate = option.State.ExpirationDate,
                        optionType = option.State.OptionType
                    )

                return ServiceResponse<OwnedOptionView>(OwnedOptionView(option.State, optionDetail = detail))
        }

    member this.Handle(query: ChainQuery) =
        task {
            let! user = accounts.GetUser(userId = query.UserId)

            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<OptionDetailsViewModel>
            | _ ->

                let! priceResult = brokerage.GetQuote(state = user.State, ticker = query.Ticker)

                let price =
                    match priceResult.IsOk with
                    | true -> Nullable<decimal>(priceResult.Success.Price)
                    | false -> Nullable<decimal>()

                let! details = brokerage.GetOptions(state = user.State, ticker = query.Ticker)

                match details.IsOk with
                | true ->
                    let model = OptionDetailsViewModel(price, details.Success)
                    return ServiceResponse<OptionDetailsViewModel>(model)
                | false -> return ServiceResponse<OptionDetailsViewModel>(ServiceError(details.Error.Message))
        }
