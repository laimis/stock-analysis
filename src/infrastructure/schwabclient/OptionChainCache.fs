module schwabclient.OptionChainCache

open System
open System.Collections.Concurrent
open core.Shared
open core.fs.Adapters.Options
open core.fs

type OptionChainCacheEntry = {
    Chain: OptionChain
    Expiration: DateTimeOffset
}

type OptionChainCache() =
    let cache = ConcurrentDictionary<string, OptionChainCacheEntry>()
    let defaultExpiration = TimeSpan.FromMinutes(5.0)
    
    member _.TryGetValue(ticker: Ticker) =
        match cache.TryGetValue(ticker.Value) with
        | true, entry ->
            if DateTimeOffset.UtcNow < entry.Expiration then
                Some entry.Chain
            else
                cache.TryRemove(ticker.Value) |> ignore
                None
        | false, _ -> None
        
    member _.Set(ticker: Ticker, chain: OptionChain) =
        let entry = {
            Chain = chain
            Expiration = DateTimeOffset.UtcNow.Add(defaultExpiration)
        }
        cache.AddOrUpdate(
            ticker.Value,
            (fun _ -> entry),
            (fun _ _ -> entry)
        ) |> ignore
