namespace dbstockinfoprovider

open System
open System.Collections.Generic
open System.Threading.Tasks
open Npgsql.FSharp
open core.Account
open core.Shared
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Options
open core.fs.Adapters.Stocks

type StockService(connectionString: string) =
    
    member this.WriteHistoricalData(ticker: string, date: DateTimeOffset, open': decimal, high: decimal, low: decimal, close: decimal, volume: int64) =
        let query = @"INSERT INTO historical_prices (ticker, date, open, high, low, close, volume) VALUES (@ticker, @date, @open, @high, @low, @close, @volume)
                      ON CONFLICT (ticker, date) DO UPDATE SET open = @open, high = @high, low = @low, close = @close, volume = @volume"
        
        connectionString
        |> Sql.connect
        |> Sql.query query
        |> Sql.parameters [
            "@ticker", ticker |> Sql.string;
            "@date", date.Date |> Sql.timestamptz;
            "@open", open' |> Sql.decimal;
            "@high", high |> Sql.decimal;
            "@low", low |> Sql.decimal;
            "@close", close |> Sql.decimal;
            "@volume", volume |> Sql.int64;
        ]
        |> Sql.executeNonQuery

    member this.WriteQuoteData(ticker: string, lastPrice: decimal, lastUpdate: DateTimeOffset) =
        let query = "INSERT INTO quotes (ticker, lastprice, lastupdate) VALUES (@ticker, @lastprice, @lastupdate) ON CONFLICT (ticker) DO UPDATE SET lastprice = @lastprice, lastupdate = @lastupdate"
        
        connectionString
        |> Sql.connect
        |> Sql.query query
        |> Sql.parameters [
            "@ticker", ticker |> Sql.string;
            "@lastprice", lastPrice |> Sql.decimal;
            "@lastupdate", lastUpdate |> Sql.timestamptz;
        ]
        |> Sql.executeNonQuery
        

    interface IStockInfoProvider with
        member this.GetQuote(state: UserState) (ticker: Ticker) : Task<Result<StockQuote, ServiceError>> = task {
            let query = "SELECT ticker,lastprice, lastupdate FROM quotes WHERE ticker = @ticker"
            
            let! quote =
                connectionString
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters ["@ticker", ticker.Value |> Sql.string]
                |> Sql.executeRowAsync (fun reader ->
                    {
                        symbol = reader.string "ticker"
                        exchange = ""
                        mark = 0m
                        volatility = 0m
                        askPrice = reader.decimal "lastprice"
                        askSize = 0m
                        bidPrice = reader.decimal "lastprice"
                        bidSize = 0m
                        closePrice = reader.decimal "lastprice"
                        exchangeName = ""
                        lastPrice = reader.decimal "lastprice"
                        lastSize = 0m
                        regularMarketLastPrice = reader.decimal "lastprice"
                        regularMarketLastSize = 0m 
                    }
                )
                
            return Ok quote
        }
                
        member this.GetQuotes(state: UserState) (tickers: Ticker seq) : Task<Result<IDictionary<Ticker, StockQuote>, ServiceError>> = task {
            
            let query = "SELECT ticker, lastprice, lastupdate FROM quotes WHERE ticker = ANY(@tickers)"
            
            let! quotes =
                connectionString
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters ["@tickers", tickers |> Seq.map (_.Value) |> Seq.toArray |> Sql.stringArray]
                |> Sql.executeAsync (fun reader ->
                    let ticker = reader.string "ticker"
                    let quote =
                        {
                            symbol = ticker
                            exchange = ""
                            mark = 0m
                            volatility = 0m
                            askPrice = reader.decimal "lastprice"
                            askSize = 0m
                            bidPrice = reader.decimal "lastprice"
                            bidSize = 0m
                            closePrice = reader.decimal "lastprice"
                            exchangeName = ""
                            lastPrice = reader.decimal "lastprice"
                            lastSize = 0m
                            regularMarketLastPrice = reader.decimal "lastprice"
                            regularMarketLastSize = 0m 
                        }
                    quote
                )
                
            let mapped = quotes |> List.map (fun q -> q.symbol |> Ticker, q) |> dict 
                
            return Ok mapped
        }

        member this.Search(state: UserState) (search: string) (limit: int) : Task<Result<SearchResult[], ServiceError>> = task {
            
            let query = "SELECT ticker FROM quotes WHERE ticker ILIKE @query LIMIT @limit"
            
            let! results =
                connectionString
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters [
                    "@query", $"%%{search}%%" |> Sql.string
                    "@limit", limit |> Sql.int
                ]
                |> Sql.executeAsync (fun reader ->
                    {
                        Symbol = reader.string "ticker"
                        SecurityName = reader.string "ticker"
                        SecurityType = "SHARE"
                        Exchange = ""
                        Region = "" 
                    }
                )
                
            return results |> List.toArray |> Ok
        }
            
        member this.GetOptions(state: UserState) (ticker: Ticker) (expirationDate: DateTimeOffset option) (strikePrice: decimal option) (contractType: string option) : Task<Result<OptionChain, ServiceError>> = task {
            return Error (ServiceError("No option data available"))
        }

        member this.GetStockProfile(state: UserState) (ticker: Ticker) : Task<Result<StockProfile, ServiceError>> = task {
            return Error (ServiceError("No stock profile data available"))
        }
        member this.GetPriceHistory(state: UserState) (ticker: Ticker) (frequency: PriceFrequency) (start: DateTimeOffset option) (``end``: DateTimeOffset option) : Task<Result<PriceBars, ServiceError>> =
            task {
                let query = "SELECT date, open, high, low, close, volume FROM historical_prices WHERE ticker = @ticker AND date >= @start AND date <= @end ORDER BY date"
                
                let! bars =
                    connectionString
                    |> Sql.connect
                    |> Sql.query query
                    |> Sql.parameters [
                        "@ticker", ticker.Value |> Sql.string
                        "@start", start.Value |> Sql.timestamptz
                        "@end", ``end``.Value |> Sql.timestamptz
                    ]
                    |> Sql.executeAsync (fun reader ->
                        PriceBar(reader.datetimeOffset "date", reader.decimal "open", reader.decimal "high", reader.decimal "low", reader.decimal "close", reader.int64 "volume")
                    )
                    
                let priceBars = bars |> List.toArray |> PriceBars
                    
                return Ok priceBars
            }
