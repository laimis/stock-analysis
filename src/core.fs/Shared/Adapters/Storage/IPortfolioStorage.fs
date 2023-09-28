namespace core.fs.Shared.Adapters.Storage

open System.Collections.Generic
open System.Threading.Tasks
open core.Cryptos
open core.Notes
open core.Options
open core.Portfolio
open core.Stocks

type IPortfolioStorage =
    
    abstract member GetStock : ticker:string -> userId:System.Guid -> Task<OwnedStock>
    abstract member GetStockByStockId : id:System.Guid -> userId:System.Guid -> Task<OwnedStock>
    abstract member GetStocks : userId:System.Guid -> Task<IEnumerable<OwnedStock>>
    abstract member Save : stock:OwnedStock -> userId:System.Guid -> Task
    
    abstract member Delete : userId:System.Guid -> Task
    
    abstract member GetStockList : name:string -> userId:System.Guid -> Task<StockList>
    abstract member GetStockLists : userId:System.Guid -> Task<IEnumerable<StockList>>
    abstract member SaveStockList : list:StockList -> userId:System.Guid -> Task
    abstract member DeleteStockList : list:StockList -> userId:System.Guid -> Task
    
    abstract member SavePendingPosition : position:PendingStockPosition -> userId:System.Guid -> Task
    abstract member GetPendingStockPositions : userId:System.Guid -> Task<IEnumerable<PendingStockPosition>>
    
    abstract member GetRoutines : userId:System.Guid -> Task<IEnumerable<Routine>>
    abstract member GetRoutine : name:string -> userId:System.Guid -> Task<Routine>
    abstract member SaveRoutine : routine:Routine -> userId:System.Guid -> Task
    abstract member DeleteRoutine : routine:Routine -> userId:System.Guid -> Task
    
    abstract member GetOwnedOptions : userId:System.Guid -> Task<IEnumerable<OwnedOption>>
    abstract member GetOwnedOption : optionId:System.Guid -> userId:System.Guid -> Task<OwnedOption>
    abstract member SaveOwnedOption : option:OwnedOption -> userId:System.Guid -> Task
    
    abstract member GetNotes : userId:System.Guid -> Task<IEnumerable<Note>>
    abstract member GetNote : noteId:System.Guid -> userId:System.Guid -> Task<Note>
    abstract member SaveNote : note:Note -> userId:System.Guid -> Task
    
    abstract member GetCrypto : token:string -> userId:System.Guid -> Task<OwnedCrypto>
    abstract member GetCryptoByCryptoId : id:System.Guid -> userId:System.Guid -> Task<OwnedCrypto>
    abstract member GetCryptos : userId:System.Guid -> Task<IEnumerable<OwnedCrypto>>
    abstract member SaveCrypto : crypto:OwnedCrypto -> userId:System.Guid -> Task