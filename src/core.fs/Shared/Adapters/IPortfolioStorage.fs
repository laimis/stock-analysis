namespace core.fs.Shared.Adapters.Storage

open System.Collections.Generic
open System.Threading.Tasks
open core.Cryptos
open core.Notes
open core.Options
open core.Portfolio
open core.Routines
open core.Shared
open core.Stocks
open core.fs.Shared.Domain.Accounts

type IPortfolioStorage =
    
    abstract member GetStock : ticker:Ticker -> userId:UserId -> Task<OwnedStock>
    abstract member GetStockByStockId : id:System.Guid -> userId:UserId -> Task<OwnedStock>
    abstract member GetStocks : userId:UserId -> Task<IEnumerable<OwnedStock>>
    abstract member Save : stock:OwnedStock -> userId:UserId -> Task
    
    abstract member Delete : userId:UserId -> Task
    
    abstract member GetStockList : name:string -> userId:UserId -> Task<StockList>
    abstract member GetStockLists : userId:UserId -> Task<IEnumerable<StockList>>
    abstract member SaveStockList : list:StockList -> userId:UserId -> Task
    abstract member DeleteStockList : list:StockList -> userId:UserId -> Task
    
    abstract member SavePendingPosition : position:PendingStockPosition -> userId:UserId -> Task
    abstract member GetPendingStockPositions : userId:UserId -> Task<IEnumerable<PendingStockPosition>>
    
    abstract member GetRoutines : userId:UserId -> Task<IEnumerable<Routine>>
    abstract member GetRoutine : name:string -> userId:UserId -> Task<Routine>
    abstract member SaveRoutine : routine:Routine -> userId:UserId -> Task
    abstract member DeleteRoutine : routine:Routine -> userId:UserId -> Task
    
    abstract member GetOwnedOptions : userId:UserId -> Task<IEnumerable<OwnedOption>>
    abstract member GetOwnedOption : optionId:System.Guid -> userId:UserId -> Task<OwnedOption>
    abstract member SaveOwnedOption : option:OwnedOption -> userId:UserId -> Task
    
    abstract member GetNotes : userId:UserId -> Task<IEnumerable<Note>>
    abstract member GetNote : noteId:System.Guid -> userId:UserId -> Task<Note>
    abstract member SaveNote : note:Note -> userId:UserId -> Task
    
    abstract member GetCrypto : token:string -> userId:UserId -> Task<OwnedCrypto>
    abstract member GetCryptoByCryptoId : id:System.Guid -> userId:UserId -> Task<OwnedCrypto>
    abstract member GetCryptos : userId:UserId -> Task<IEnumerable<OwnedCrypto>>
    abstract member SaveCrypto : crypto:OwnedCrypto -> userId:UserId -> Task