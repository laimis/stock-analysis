namespace coinmarketcap

open System
open System.Collections.Generic
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open core.fs.Adapters.Cryptos

type CoinMarketCapClient(logger: ILogger<CoinMarketCapClient> option, accessToken: string) =
    
    static let httpClient = new HttpClient()
    
    do
        if not (httpClient.DefaultRequestHeaders.Contains("X-CMC_PRO_API_KEY")) then
            httpClient.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", accessToken)
    
    member _.GetAll() : Task<Listings> =
        task {
            let url = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest"
            
            let! response = httpClient.GetAsync(url)
            let! content = response.Content.ReadAsStringAsync()
            
            if not response.IsSuccessStatusCode then
                raise (Exception($"Could not get listings: {content}"))
            
            let value = JsonSerializer.Deserialize<Listings>(content)
            
            if isNull (box value) then
                raise (Exception($"Could not deserialize response: {content}"))
            
            return value
        }
    
    member this.Get(token: string) : Task<Datum option> =
        task {
            let! prices = this.GetAll()
            return prices.TryGet(token)
        }
    
    member this.Get(tokens: string seq) : Task<Dictionary<string, Datum>> =
        task {
            let! prices = this.GetAll()
            
            let result = Dictionary<string, Datum>()
            
            for token in tokens do
                match prices.TryGet(token) with
                | Some datum ->
                    result.Add(token, datum)
                | None ->
                    match logger with
                    | Some log -> log.LogError($"Did not find price for {token}")
                    | None -> ()
            
            return result
        }
    
    interface ICryptoService with
        member this.GetAll() = this.GetAll()
        member this.Get(token: string) = this.Get(token)
        member this.Get(tokens: string seq) = this.Get(tokens)
