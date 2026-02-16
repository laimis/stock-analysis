namespace storage.shared

module EventInfraAdjustments =
    
    // This module is used to change event JSON to match the namespaces properly after refactoring events.
    // JSON serialization is using namespace paths and those can change after the events are moved.
    // We rewrite JSON on the fly, or we can also use this to fix events when we persist in the database during
    // migrations.
    
    let AdjustIfNeeded (json: string) : string =
        let mutable result = json
        
        // handling Routines move from core.Portfolio to core.Routines
        if result.Contains("\"$type\":\"core.Portfolio.Routine") then
            result <- result.Replace("\"$type\":\"core.Portfolio.Routine", "\"$type\":\"core.Routines.Routine")
        
        if result.Contains("\"$type\":\"core.Portfolio.StockList") then
            result <- result.Replace("\"$type\":\"core.Portfolio.StockList", "\"$type\":\"core.Stocks.StockList")
        
        if result.Contains("\"$type\":\"core.Portfolio.PendingStockPosition") then
            result <- result.Replace("\"$type\":\"core.Portfolio.PendingStockPosition", "\"$type\":\"core.Stocks.PendingStockPosition")
        
        result
