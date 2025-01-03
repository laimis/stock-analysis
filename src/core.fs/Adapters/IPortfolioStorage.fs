namespace core.fs.Adapters.Storage

open System.Collections.Generic
open System.Threading.Tasks
open core.Cryptos
open core.Options
open core.Routines
open core.Stocks
open core.fs.Accounts
open core.fs.Options
open core.fs.Stocks

type IPortfolioStorage =
    
    
    abstract member GetStockPositions : userId:UserId -> Task<IEnumerable<StockPositionState>>
    abstract member GetStockPosition : positionId:StockPositionId -> userId:UserId -> Task<StockPositionState option>
    abstract member SaveStockPosition : userId:UserId -> previousState:StockPositionState option -> newState:StockPositionState -> Task
    abstract member DeleteStockPosition : userId:UserId -> previousState:StockPositionState option -> newState:StockPositionState -> Task
    
    abstract member Delete : userId:UserId -> Task
    
    abstract member GetStockList : Id:System.Guid -> userId:UserId -> Task<StockList>
    abstract member GetStockLists : userId:UserId -> Task<IEnumerable<StockList>>
    abstract member SaveStockList : list:StockList -> userId:UserId -> Task
    abstract member DeleteStockList : list:StockList -> userId:UserId -> Task
    
    abstract member SavePendingPosition : position:PendingStockPosition -> userId:UserId -> Task
    abstract member GetPendingStockPositions : userId:UserId -> Task<IEnumerable<PendingStockPosition>>
    
    abstract member GetRoutines : userId:UserId -> Task<IEnumerable<Routine>>
    abstract member GetRoutine : routineId:System.Guid -> userId:UserId -> Task<Routine>
    abstract member SaveRoutine : routine:Routine -> userId:UserId -> Task
    abstract member DeleteRoutine : routine:Routine -> userId:UserId -> Task
    
    abstract member GetOwnedOptions : userId:UserId -> Task<IEnumerable<OwnedOption>>
    abstract member GetOwnedOption : optionId:System.Guid -> userId:UserId -> Task<OwnedOption>
    abstract member SaveOwnedOption : option:OwnedOption -> userId:UserId -> Task
    
    abstract member SaveOptionPosition : userId:UserId -> previousState:OptionPositionState option -> newState:OptionPositionState -> Task
    
    abstract member GetCrypto : token:string -> userId:UserId -> Task<OwnedCrypto>
    abstract member GetCryptoByCryptoId : id:System.Guid -> userId:UserId -> Task<OwnedCrypto>
    abstract member GetCryptos : userId:UserId -> Task<IEnumerable<OwnedCrypto>>
    abstract member SaveCrypto : crypto:OwnedCrypto -> userId:UserId -> Task
