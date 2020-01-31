import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';

export function GetErrors(err:any): string[] {
  var objToMap = err.error.errors
  if (objToMap === undefined)
  {
    objToMap = err.error
  }

  if (typeof(objToMap) === 'string')
  {
    return [objToMap]
  }

  return Object.keys(objToMap).map<string>(v => {
    return Object.getOwnPropertyDescriptor(objToMap, v).value
  })
}

@Injectable({providedIn: 'root'})
export class StocksService {

  constructor(private http: HttpClient) { }

  // ----------------- misc ---------------------

  getEvents(type:string): Observable<object[]> {
    return this.http.get<object[]>('/api/events?entity=' + type)
  }

  getReviewEntires(): Observable<ReviewList> {
    return this.http.get<ReviewList>('/api/portfolio/review')
  }

  getTransactions(ticker:string): Observable<TransactionList> {
    if (ticker === null)
    {
      ticker = ''
    }
    return this.http.get<TransactionList>('/api/portfolio/transactions?ticker=' + ticker)
  }

  // ----------------- notes ---------------------

  addNote(input: any): Observable<any> {
    return this.http.post<any>('/api/notes', input)
  }

  saveNote(note: object) {
    return this.http.patch<any>('/api/notes', note)
  }

  importNotes(formData: FormData) {
    return this.http.post('/api/notes/import', formData)
  }

  getNotes(ticker: string): Observable<NoteList> {
    if (ticker === null)
    {
      ticker = ''
    }

    return this.http.get<NoteList>('/api/notes?ticker=' + ticker)
  }

  getNote(id: string): Observable<object> {
    return this.http.get<object>('/api/notes/' + id)
  }

  // ------------------ stocks ------------------

	getStockLists(): Observable<StockLists> {
    return this.http.get<StockLists>('/api/stocks/lists')
  }

	getStockSummary(symbol:string): Observable<StockSummary> {
		return this.http.get<StockSummary>('/api/stocks/' + symbol)
  }

  importStocks(file: any) : Observable<any> {
    return this.http.post('/api/stocks/import', file)
  }

	purchase(obj:object) : Observable<any> {
		return this.http.post('/api/stocks/purchase', obj)
	}

	sell(obj:object) : Observable<any> {
		return this.http.post('/api/stocks/sell', obj)
  }

  search(term: string): Observable<object[]> {
    if (!term.trim()) {
      return of([]);
    }
    return this.http.get<object[]>(`/api/stocks/search/${term}`).pipe(
      tap(_ => console.log(`found stoks matching "${term}"`))
    );
  }

  // ------- portfolio ----------------

	getPortfolio(): Observable<Portfolio> {
		return this.http.get<Portfolio>('/api/portfolio')
  }

  // ------- options ----------------

	buyOption(obj:object) : Observable<any> {
		return this.http.post<string>('/api/options/buy', obj)
  }

  sellOption(obj:object) : Observable<any> {
		return this.http.post<string>('/api/options/sell', obj)
  }

  getOption(id:string) : Observable<OptionDefinition> {
    return this.http.get<OptionDefinition>('/api/options/' + id)
  }

  closeOption(obj:object) : Observable<any> {
    return this.http.post('/api/options/close', obj)
  }

  getOptions(ticker:string): Observable<OptionDetail> {
    return this.http.get<OptionDetail>('/api/options/' + ticker + '/details')
  }

  importOptions(formData: FormData) {
    return this.http.post('/api/options/import', formData)
  }

  // ---------- accounts ---------

  getAccountStatus() : Observable<AccountStatus> {
    return this.http.get<AccountStatus>('/api/account/status')
  }

  getProfile() : Observable<object> {
    return this.http.get<object>('/api/account')
  }

  createAccount(obj:object) : Observable<object> {
    return this.http.post<object>('/api/account', obj)
  }

  loginAccount(obj:object) : Observable<object> {
    return this.http.post<object>('/api/account/login', obj)
  }

  requestPasswordReset(obj:object) : Observable<object> {
    return this.http.post<object>('/api/account/requestpasswordreset', obj)
  }

  deleteAccount(obj:object) : Observable<object> {
    return this.http.post('/api/account/delete', obj)
  }
}

export interface ReviewList {
  transactionList: TransactionList,
  tickers: object[]
}

export interface TransactionList {
  transactions: object[]
}
export interface NoteList {
  notes: object[]
}

export interface StockLists {
  active : object[]
  gainers: object[]
  losers: object[]
}

export interface Portfolio {
  owned: object[]
  openOptions: object[]
}

export interface StockSummary {
  price: number,
  stats: object,
  profile : object
	priceChartData : object[]
	volumeChartData : object[]
	peChartData : object[]
	bookChartData : object[]
}

export class OptionDefinition {
  id: string
  ticker: string
  strikePrice: number
  expirationDate: string
  optionType: string
  numberOfContracts: number
  bid: number
  premium: number
  filled: string
  breakEven: number
  risk: number
  volume : number
}

export class OptionBreakdown {
  callVolume : number
  putVolume : number
  callSpend : number
  putSpend : number
  priceBasedOnCalls : number
  priceBasedOnPuts : number
}

export interface OptionDetail {
  stockPrice: number
  lastUpdated: string
  expirations: string[]
  options: OptionDefinition[]
  breakdown : OptionBreakdown
}

export interface AccountStatus {
  username: String
  loggedIn: boolean
}
