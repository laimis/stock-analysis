namespace storage.postgres

open System
open System.Data
open System.Linq
open core.fs.Accounts
open core.Shared
open Dapper
open Microsoft.FSharp.Core
open Newtonsoft.Json
open Npgsql
open storage.shared

type PostgresAggregateStorage(outbox: IOutbox, connectionString: string) =
    
    let dataSource = (new NpgsqlDataSourceBuilder(connectionString)).Build()
    
    member _.GetConnection() : IDbConnection =
        dataSource.OpenConnection()
    
    interface IAggregateStorage with
        
        member this.DeleteAggregates(entity: string, userId: UserId, outsideTransaction: IDbTransaction) = 
            task {
                let mutable db: IDbConnection = null
                let mutable tx: IDbTransaction = null
                let ownsConnection = isNull outsideTransaction
                
                try
                    if not (isNull outsideTransaction) then
                        db <- outsideTransaction.Connection
                        tx <- outsideTransaction
                    else
                        db <- this.GetConnection()
                        tx <- db.BeginTransaction()
                    
                    let (UserId id) = userId
                    let sql = "DELETE FROM events WHERE entity = @entity AND userId = @userId"
                    let! _ = db.ExecuteAsync(sql, {| userId = id; entity = entity |})
                    
                    if ownsConnection then
                        tx.Commit()
                with ex ->
                    if ownsConnection && not (isNull tx) then
                        try tx.Rollback() with _ -> ()
                    raise ex
                
                if ownsConnection then
                    if not (isNull tx) then
                        try tx.Dispose() with _ -> ()
                    if not (isNull db) then
                        try db.Dispose() with _ -> ()
            }
        
        member this.DeleteAggregate(entity: string, aggregateId: Guid, userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                let sql = "DELETE FROM events WHERE entity = @entity AND userId = @userId AND aggregateId = @aggregateId"
                let! _ = db.ExecuteAsync(sql, {| userId = id; entity = entity; aggregateId = aggregateId.ToString() |})
                return ()
            }
        
        member this.GetEventsAsync(entity: string, userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                let sql = "select * FROM events WHERE entity = @entity AND userId = @userId ORDER BY version"
                let! list = db.QueryAsync<StoredAggregateEvent>(sql, {| entity = entity; userId = id |})
                
                return list.Select(fun e -> StoredAggregateEvent.deserializeEvent(e.EventJson))
            }
        
        member this.GetEventsAsync(entity: string, aggregateId: Guid, userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                let sql = "select * FROM events WHERE entity = @entity AND userId = @userId AND aggregateId = @aggregateId ORDER BY version"
                let! list = db.QueryAsync<StoredAggregateEvent>(sql, {| entity = entity; userId = id; aggregateId = aggregateId.ToString() |})
                
                return list.Select(fun e -> StoredAggregateEvent.deserializeEvent(e.EventJson))
            }
        
        member this.SaveEventsAsync(agg: IAggregate, entity: string, userId: UserId, outsideTransaction: IDbTransaction) = 
            this.SaveEventsAsyncInternal(agg, agg.Version, entity, userId, outsideTransaction)
        
        member this.SaveEventsAsync(oldAggregate: IAggregate, newAggregate: IAggregate, entity: string, userId: UserId, outsideTransaction: IDbTransaction) = 
            let fromVersion = if isNull oldAggregate then 0 else oldAggregate.Version
            this.SaveEventsAsyncInternal(newAggregate, fromVersion, entity, userId, outsideTransaction)
        
        member this.DoHealthCheck() = 
            task {
                use db = this.GetConnection()
                let! _ = db.QueryAsync<int>("select 1")
                return ()
            }
    
    member private this.SaveEventsAsyncInternal(agg: IAggregate, fromVersion: int, entity: string, userId: UserId, outsideTransaction: IDbTransaction) = 
        task {
            let mutable db: IDbConnection = null
            let mutable tx: IDbTransaction = null
            let ownsConnection = isNull outsideTransaction
            
            try
                if not (isNull outsideTransaction) then
                    db <- outsideTransaction.Connection
                    tx <- outsideTransaction
                else
                    db <- this.GetConnection()
                    tx <- db.BeginTransaction()
                
                let mutable version = fromVersion
                let eventsToBlast = System.Collections.Generic.List<AggregateEvent>()
                let (UserId id) = userId
                
                for e in agg.Events.Skip(fromVersion) do
                    version <- version + 1
                    let se = StoredAggregateEvent.create entity id (e.AggregateId.ToString()) (e.Id.ToString()) DateTimeOffset.UtcNow version e
                    
                    let query = "INSERT INTO events (entity, key, aggregateid, userid, created, version, eventjson) VALUES (@Entity, @Key, @AggregateId, @UserId, @Created, @Version, @EventJson)"
                    
                    let! _ = db.ExecuteAsync(query, se)
                    eventsToBlast.Add(e)
                
                let! result = outbox.AddEvents(eventsToBlast, tx)
                match result with
                | Ok _ -> ()
                | Error e -> failwithf "Failed to add events to outbox: %A" e
                
                if ownsConnection then
                    tx.Commit()
            with ex ->
                if ownsConnection && not (isNull tx) then
                    try tx.Rollback() with _ -> ()
                if ownsConnection then
                    if not (isNull tx) then try tx.Dispose() with _ -> ()
                    if not (isNull db) then try db.Dispose() with _ -> ()
                raise ex
            
            if ownsConnection then  
                if not (isNull tx) then  
                    try tx.Dispose() with _ -> ()  
                if not (isNull db) then  
                    try db.Dispose() with _ -> ()  
        }
    
    member this.GetStoredEvents(entity: string, userId: UserId) = 
        task {
            use db = this.GetConnection()
            let (UserId id) = userId
            let sql = "select * FROM events WHERE entity = @entity AND userId = @userId ORDER BY key, version"
            let! list = db.QueryAsync<StoredAggregateEvent>(sql, {| entity = entity; userId = id |})
            return list
        }
    
    interface IBlobStorage with
        
        member this.Get<'T>(key: string) = 
            task {
                use db = this.GetConnection()
                let sql = "select blob FROM blobs WHERE key = @key"
                let! blob = db.QuerySingleOrDefaultAsync<string>(sql, {| key = key |})
                
                if String.IsNullOrEmpty(blob) then
                    return None
                else
                    let value = JsonConvert.DeserializeObject<'T>(blob)
                    return Some(value)
            }
        
        member this.Save<'T>(key: string, t: 'T) = 
            task {
                let blob = JsonConvert.SerializeObject(t)
                use db = this.GetConnection()
                
                let sql = "INSERT INTO blobs (key, blob, inserted) VALUES (@key, @blob, current_timestamp) ON CONFLICT (key) DO UPDATE SET blob = @blob, inserted = current_timestamp"
                let! _ = db.ExecuteAsync(sql, {| key = key; blob = blob |})
                return ()
            }
