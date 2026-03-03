namespace core.fs.Stocks

open System
open core.Shared
open core.fs.Accounts

type StockListTicker =
    {
        Ticker: Ticker
        Note: string
        When: DateTimeOffset
    }

type StockList =
    {
        Id: Guid
        UserId: UserId
        Name: string
        Description: string
        Tickers: StockListTicker list
        Tags: string list
    }

    member this.ContainsTag(tag: string) = this.Tags |> List.contains tag
