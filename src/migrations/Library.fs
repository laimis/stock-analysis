namespace migrations

open CsvHelper.Configuration.Attributes
open core.Stocks
open core.fs.Shared.Adapters.Logging
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain
open core.fs.Shared.Domain.Accounts
open storage.shared

// we will be migrating from OwnedStockState silliness to StockPosition
module MigrateFromV2ToV3 =
    
    let mapToStockPositionEvent (t:PositionEvent) =
        match t.Type.Value with
        | PositionEventType.Buy -> fun sp -> sp |> StockPosition.buy t.Quantity.Value t.Value.Value t.When (Some t.Notes)
        | PositionEventType.Sell -> fun sp -> sp |> StockPosition.sell t.Quantity.Value t.Value.Value t.When (Some t.Notes)
        | PositionEventType.Stop ->
            match t.Value.HasValue with
            | true -> (fun sp -> sp |> StockPosition.setStop (Some t.Value.Value) t.When) 
            | false -> (fun sp -> sp |> StockPosition.deleteStop t.When)
        | PositionEventType.Risk -> fun sp -> sp |> StockPosition.setRiskAmount t.Value.Value t.When
        | _ -> failwith ("not implemented" + t.Type.Value)
                
    let migrateUser (storage:PortfolioStorage) (logger:ILogger) (user:User) = async {
        let userId = user.State.Id |> UserId
        
        let! stocks = userId |> storage.GetStocks |> Async.AwaitTask
        
        logger.LogInformation($"Migrating {user.State.Email}")
        logger.LogInformation($"Found {stocks |> Seq.length} stocks")
        
        let asyncs =
            stocks
            |> Seq.collect( fun s ->
                
                logger.LogInformation($"Migrating {s.State.Ticker} {s.State.GetAllPositions().Count} positions")
                s.State.GetAllPositions()
            )
            |> Seq.map( fun toMigrate -> async {
               
                    let initialPosition = StockPosition.openLong toMigrate.Ticker toMigrate.Opened 
                    let functionSequences =
                        toMigrate.Events
                        |> Seq.map mapToStockPositionEvent
                        
                    let migratedPosition =
                        functionSequences
                        |> Seq.fold (fun sp f -> f sp) initialPosition
                        |> fun x ->
                            match toMigrate.Grade.HasValue with
                            | true ->
                                let gradeNote =
                                    match toMigrate.GradeNote = null with
                                    | true -> None
                                    | false -> Some toMigrate.GradeNote
                                    
                                x |> StockPosition.assignGrade toMigrate.Grade.Value gradeNote toMigrate.GradeDate.Value
                            | false -> x
                            
                    do! storage.SaveStockPosition(userId, None , migratedPosition) |> Async.AwaitTask
                    
                    return migratedPosition
                }
            )
            
        return! asyncs |> Async.Sequential
    }
    
    let migrate (storage:PortfolioStorage) (accounts:IAccountStorage) (logger:ILogger) = task {
        let! emailPairs = accounts.GetUserEmailIdPairs()
        
        logger.LogInformation($"Found {emailPairs |> Seq.length} users to migrate")
        
        let! users =
            emailPairs
            |> Seq.map (fun emailIdPair -> async {
                let! user = emailIdPair.Id |> accounts.GetUser |> Async.AwaitTask
                return user
                })
            |> Async.Sequential
        
        return! users
        |> Seq.choose id
        |> Seq.map (migrateUser storage logger)
        |> Async.Sequential
        |> Async.StartAsTask
    }