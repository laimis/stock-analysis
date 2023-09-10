namespace core.fs.Options

open core.Shared

module Dashboard =
    open core.Account
    open core.Shared.Adapters.Brokerage
    open core
    open core.Options
    open core.fs

    [<Struct>]
    type Query(userId:System.Guid) =
        member _.UserId = userId

    type Handler(accounts: IAccountStorage, brokerage: IBrokerage, storage: IPortfolioStorage) =
        interface IApplicationService
        member this.Handle(request:Query) =
            task {
                let! user = request.UserId |> accounts.GetUser
                
                let! options = storage.GetOwnedOptions(user.Id);

                let closedOptions =
                    options
                    |> Seq.filter( fun o -> o.State.Closed.HasValue)
                    |> Seq.map(fun o -> o.State)
                    |> Seq.sortByDescending(fun o -> o.FirstFill.Value)
                    |> Seq.map(fun o -> OwnedOptionView(o, null))

                let openOptionsTasks =
                    options
                    |> Seq.filter(fun o -> o.State.Closed.HasValue |> not)
                    |> Seq.map(fun o -> o.State)
                    |> Seq.sortBy(fun o -> o.Ticker, o.Expiration)
                    |> Seq.map( fun o ->
                        task {
                            let! chain = brokerage.GetOptions(user.State, o.Ticker, o.Expiration)
                            let detail = 
                                match chain.IsOk with
                                | true -> chain.Success.FindMatchingOption(o.StrikePrice, o.ExpirationDate, o.OptionType)
                                | false -> null
                            return OwnedOptionView(o, detail)
                        }
                    )

                let! openOptions = System.Threading.Tasks.Task.WhenAll(openOptionsTasks)

                let! brokerageAccount = brokerage.GetAccount(user.State);

                let brokeragePositions =
                    match brokerageAccount.IsOk with
                    | true -> 
                        brokerageAccount.Success.OptionPositions
                        |> Seq.filter(fun p -> openOptions |> Seq.exists(fun o -> o.Ticker = p.Ticker && o.StrikePrice = p.StrikePrice && o.OptionType = p.OptionType) |> not)
                    | false -> Seq.empty

                let brokerageOrders = 
                    match brokerageAccount.IsOk with
                    | true -> 
                        brokerageAccount.Success.Orders
                    | false ->
                        Array.empty

                let view = OptionDashboardView(closedOptions, openOptions, brokeragePositions, brokerageOrders)
                
                return ServiceResponse<OptionDashboardView>(view)
            }