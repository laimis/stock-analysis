namespace core.fs.Adapters.Options

open System
open core.fs.Options

type OptionDetail(symbol:string, optionType:OptionType, description:string, expiration:OptionExpiration) =
    
    member this.Symbol = symbol
    member this.Description = description
    member this.Expiration: OptionExpiration = expiration
    member val StrikePrice: decimal = 0m with get, set
    member val Volume: int64 = 0L with get, set
    member val OpenInterest: int64 = 0L with get, set
    member val Bid: decimal = 0m with get, set
    member val Ask: decimal = 0m with get, set
    member val Last: decimal = 0m with get, set
    member val Mark: decimal = 0m with get, set
    member this.OptionType = optionType
    member this.IsCall = match optionType with | Call -> true | _ -> false
    member this.IsPut = match optionType with | Put -> true | _ -> false
    member this.Spread = this.Ask - this.Bid
    member val Volatility: decimal = 0m with get, set
    member val Delta: decimal = 0m with get, set
    member val Gamma: decimal = 0m with get, set
    member val Theta: decimal = 0m with get, set
    member val Vega: decimal = 0m with get, set
    member val Rho: decimal = 0m with get, set
    member val TimeValue: decimal = 0m with get, set
    member val IntrinsicValue: decimal = 0m with get, set
    member val InTheMoney: bool = false with get, set
    member val ExchangeName: string option = None with get, set
    member val DaysToExpiration: int = 0 with get, set
    member val MarkChange: decimal = 0m with get, set
    member val MarkPercentChange: decimal = 0m with get, set
    member val UnderlyingPrice: decimal option = None with get, set
    
    member this.PerDayPrice =
        
        let date = this.Expiration.ToDateTimeOffset().Date
        let today = DateTimeOffset.UtcNow.Date
        let diff = Math.Max(int(date.Subtract(today).TotalDays), 1)
        this.Bid * 100m / decimal(diff)
        
    member this.BreakEven =
        
        if this.IsCall then
            this.StrikePrice + this.Bid
        else
            this.StrikePrice - this.Bid
            
    member this.Risk =
        
        this.Bid / this.StrikePrice


type OptionChain(symbol: string, volatility: decimal, numberOfContracts: decimal, options: OptionDetail[], underlyingPrice: decimal option) =
    member this.Symbol = symbol
    member this.Volatility = volatility
    member this.NumberOfContracts = numberOfContracts
    member this.Options = options
    member this.UnderlyingPrice = underlyingPrice

    member this.FindMatchingOption(strikePrice: decimal, expirationDate: OptionExpiration, optionType: OptionType) =
        options
        |> Seq.tryFind(fun o -> o.StrikePrice = strikePrice && o.Expiration = expirationDate && o.OptionType = optionType)
