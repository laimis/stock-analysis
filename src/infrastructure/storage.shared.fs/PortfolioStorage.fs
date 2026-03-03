namespace storage.shared

open System
open System.Linq
open System.Threading.Tasks
open core.Cryptos
open core.fs.Accounts
open core.fs.Adapters.Storage
open core.fs.Options
open core.fs.Stocks
open core.Routines
open core.Shared
open core.Stocks

type PortfolioStorage(aggregateStorage: IAggregateStorage, blobStorage: IBlobStorage) =
    
    // Entity type constants
    let _stock_entity = "ownedstock3"
    let _option_entity = "soldoption3"
    let _crypto_entity = "ownedcrypto"
    let _routine_entity = "routine"
    let _pending_stock_position_entity = "pendingstockposition"
    let _stock_position_entity = "stockposition"
    let _option_position_entity = "optionposition"
    let _note_entity = "note3" // used to be used for stocks but it was modeled "weirdly" and ditched
    
    member private _.Save(agg: IAggregate, entityName: string, userId: UserId) : Task =
        aggregateStorage.SaveEventsAsync(agg, entityName, userId, null)
    
    member private _.SaveEntityInternal<'T when 'T :> IAggregate>
        (userId: UserId, previousState: 'T option, newState: 'T, entityType: string) : Task =
        match previousState with
        | None -> aggregateStorage.SaveEventsAsync(null, newState, entityType, userId, null)
        | Some prev -> aggregateStorage.SaveEventsAsync(prev, newState, entityType, userId, null)
    
    // Stock methods
    member internal this.GetStock(ticker: Ticker, userId: UserId) : Task<OwnedStock> = task {
        let! stocks = this.GetStocks(userId)
        return stocks |> Seq.tryFind (fun s -> s.State.Ticker.Equals(ticker)) |> Option.toObj
    }
    
    member internal this.GetStockByStockId(stockId: Guid, userId: UserId) : Task<OwnedStock> = task {
        let! stocks = this.GetStocks(userId)
        return stocks |> Seq.tryFind (fun s -> s.Id = stockId) |> Option.toObj
    }
    
    member internal this.Save(stock: OwnedStock, userId: UserId) : Task =
        this.Save(stock, _stock_entity, userId)
    
    member internal this.GetStocks(userId: UserId) : Task<seq<OwnedStock>> = task {
        let! list = aggregateStorage.GetEventsAsync(_stock_entity, userId)
        return list.GroupBy(fun e -> e.AggregateId)
                   .Select(fun g -> OwnedStock(g))
    }
    
    // Stock position methods
    member this.GetStockPositions(userId: UserId) : Task<seq<StockPositionState>> = task {
        let! events = aggregateStorage.GetEventsAsync(_stock_position_entity, userId)
        return events.GroupBy(fun e -> e.AggregateId)
                     .Select(StockPosition.createFromEvents)
    }
    
    member this.GetStockPosition(positionId: StockPositionId) (userId: UserId) : Task<StockPositionState option> = task {
        let (StockPositionId guid) = positionId
        let! events = aggregateStorage.GetEventsAsync(_stock_position_entity, guid, userId)
        let list = events |> Seq.toList
        
        return
            if list.IsEmpty then
                None
            else
                Some (StockPosition.createFromEvents list)
    }
    
    member this.SaveStockPosition (userId: UserId) (previousState: StockPositionState option) (newState: StockPositionState) : Task =
        this.SaveEntityInternal(userId, previousState, newState, _stock_position_entity)
    
    member this.DeleteStockPosition (userId: UserId) (previousState: StockPositionState option) (state: StockPositionState) : Task = task {
        // first save so that we persist deleted event
        do! this.SaveStockPosition userId previousState state
        let (StockPositionId guid) = state.PositionId
        do! aggregateStorage.DeleteAggregate(_stock_position_entity, guid, userId)
    }
    
    // Option position methods
    member this.GetOptionPosition(positionId: OptionPositionId) (userId: UserId) : Task<OptionPositionState option> = task {
        let (OptionPositionId guid) = positionId
        let! positions = aggregateStorage.GetEventsAsync(_option_position_entity, guid, userId)
        let list = positions |> Seq.toList
        
        return
            if list.IsEmpty then
                None
            else
                Some (OptionPosition.createFromEvents list)
    }
    
    member this.GetOptionPositions(userId: UserId) : Task<seq<OptionPositionState>> = task {
        let! events = aggregateStorage.GetEventsAsync(_option_position_entity, userId)
        return events.GroupBy(fun e -> e.AggregateId)
                     .Select(OptionPosition.createFromEvents)
    }
    
    member this.SaveOptionPosition (userId: UserId) (previousState: OptionPositionState option) (newState: OptionPositionState) : Task =
        this.SaveEntityInternal(userId, previousState, newState, _option_position_entity)
    
    member this.DeleteOptionPosition (userId: UserId) (previousState: OptionPositionState option) (newState: OptionPositionState) : Task = task {
        // first save so that we persist deleted event
        do! this.SaveOptionPosition userId previousState newState
        let (OptionPositionId guid) = newState.PositionId
        do! aggregateStorage.DeleteAggregate(_option_position_entity, guid, userId)
    }
    
    // Crypto methods
    member this.GetCrypto (token: string) (userId: UserId) : Task<OwnedCrypto> = task {
        let! cryptos = this.GetCryptos(userId)
        return cryptos |> Seq.tryFind (fun s -> s.State.Token = token) |> Option.toObj
    }
    
    member this.GetCryptoByCryptoId (cryptoId: Guid) (userId: UserId) : Task<OwnedCrypto> = task {
        let! cryptos = this.GetCryptos(userId)
        return cryptos |> Seq.tryFind (fun s -> s.Id = cryptoId) |> Option.toObj
    }
    
    member this.SaveCrypto (crypto: OwnedCrypto) (userId: UserId) : Task =
        this.Save(crypto, _crypto_entity, userId)
    
    member this.GetCryptos(userId: UserId) : Task<seq<OwnedCrypto>> = task {
        let! list = aggregateStorage.GetEventsAsync(_crypto_entity, userId)
        return list.GroupBy(fun e -> e.AggregateId)
                   .Select(fun g -> OwnedCrypto(g))
    }
    
    // Routine methods
    member this.GetRoutines(userId: UserId) : Task<seq<Routine>> = task {
        let! list = aggregateStorage.GetEventsAsync(_routine_entity, userId)
        return list.GroupBy(fun e -> e.AggregateId)
                   .Select(fun g -> Routine(g))
    }
    
    member this.SaveRoutine (routine: Routine) (userId: UserId) : Task =
        this.Save(routine, _routine_entity, userId)
    
    member this.GetRoutine (id: Guid) (userId: UserId) : Task<Routine> = task {
        let! list = this.GetRoutines(userId)
        return list |> Seq.tryFind (fun s -> s.State.Id = id) |> Option.toObj
    }
    
    member this.DeleteRoutine (routine: Routine) (userId: UserId) : Task =
        aggregateStorage.DeleteAggregate(_routine_entity, routine.Id, userId)
    
    // Pending position methods
    member this.SavePendingPosition (position: PendingStockPosition) (userId: UserId) : Task =
        this.Save(position, _pending_stock_position_entity, userId)
    
    member this.GetPendingStockPositions(userId: UserId) : Task<seq<PendingStockPosition>> = task {
        let! events = aggregateStorage.GetEventsAsync(_pending_stock_position_entity, userId)
        return events.GroupBy(fun e -> e.AggregateId)
                     .Select(fun g -> PendingStockPosition(g))
    }
    
    // Delete all user data
    member this.Delete(userId: UserId) : Task = task {
        do! aggregateStorage.DeleteAggregates(_note_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_option_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_option_position_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_stock_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_crypto_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_pending_stock_position_entity, userId, null)
        do! aggregateStorage.DeleteAggregates(_stock_position_entity, userId, null)
    }
    
    interface IPortfolioStorage with
        member this.GetStockPositions userId = this.GetStockPositions userId
        member this.GetStockPosition positionId userId = this.GetStockPosition positionId userId
        member this.SaveStockPosition userId previousState newState = this.SaveStockPosition userId previousState newState
        member this.DeleteStockPosition userId previousState state = this.DeleteStockPosition userId previousState state
        member this.GetOptionPosition positionId userId = this.GetOptionPosition positionId userId
        member this.GetOptionPositions userId = this.GetOptionPositions userId
        member this.SaveOptionPosition userId previousState newState = this.SaveOptionPosition userId previousState newState
        member this.DeleteOptionPosition userId previousState newState = this.DeleteOptionPosition userId previousState newState
        member this.Delete userId = this.Delete userId
        member this.SavePendingPosition position userId = this.SavePendingPosition position userId
        member this.GetPendingStockPositions userId = this.GetPendingStockPositions userId
        member this.GetRoutines userId = this.GetRoutines userId
        member this.GetRoutine routineId userId = this.GetRoutine routineId userId
        member this.SaveRoutine routine userId = this.SaveRoutine routine userId
        member this.DeleteRoutine routine userId = this.DeleteRoutine routine userId
        member this.GetCrypto token userId = this.GetCrypto token userId
        member this.GetCryptoByCryptoId id userId = this.GetCryptoByCryptoId id userId
        member this.GetCryptos userId = this.GetCryptos userId
        member this.SaveCrypto crypto userId = this.SaveCrypto crypto userId
