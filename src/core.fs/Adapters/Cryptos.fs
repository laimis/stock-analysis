namespace core.fs.Adapters.Cryptos

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Shared

    
[<CLIMutable>]
type Platform =
    {
        id: int
        name: string
        symbol: string
        slug: string
        token_address: string
    }

[<CLIMutable>]
type Usd =
    {
        price: decimal
        volume_24h: double
        percent_change_1h: double
        percent_change_24h: double
        percent_change_7d: double
        percent_change_30d: double
        percent_change_60d: double
        percent_change_90d: double
        market_cap: double
        market_cap_dominance: double
        fully_diluted_market_cap: double
        last_updated: DateTimeOffset
    }

[<CLIMutable>]
type Quote =
    {
        usd: Usd
    }
    
[<CLIMutable>]
type Datum =
    {
        id: int
        name: string
        symbol: string
        slug: string
        num_market_pairs: int
        date_added: DateTimeOffset
        tags: string list
        max_supply: int option
        circulating_supply: double
        total_supply: double
        platform: Platform option
        cmc_rank: int
        last_updated: DateTimeOffset
        quote: Quote
    }

[<CLIMutable>]
type Status =
    {
        timestamp: DateTimeOffset
        error_code: int
        error_message: string
        elapsed: int
        credit_count: int
        notice: string
        total_count: int
    }
    
[<CLIMutable>]
type Listings =
    {
        status: Status
        data: Datum list
    }
    
    with
        member this.TryGet(token: string) =
            let data = this.data |> List.tryFind (fun d -> d.symbol = token)
            match data with
            | Some d -> Some (Price(d.quote.usd.price))
            | None -> None


type ICryptoService =
    abstract member GetAll : unit -> Task<Listings>
    abstract member Get : token:string -> Task<Price option>
    abstract member Get : tokens:string seq -> Task<Dictionary<string, Price>>
