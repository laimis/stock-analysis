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

export function HideIfHidden(value, hidden) {
  return hidden ? 0 : value;
}

@Injectable({providedIn: 'root'})
export class StocksService {

  constructor(private http: HttpClient) { }

  // ----------------- misc ---------------------

  registerForTracking(ticker:string): Observable<TrackingPreview> {
    return this.http.get<any>('/api/tracking/' + ticker + '/register')
  }

  getEvents(type:string): Observable<object[]> {
    return this.http.get<object[]>('/api/events?entity=' + type)
  }

  getReviewEntires(period:string): Observable<ReviewList> {
    return this.http.get<ReviewList>('/api/portfolio/review?period=' + period)
  }

  getTradingEntries(): Observable<StockTradingGridEntry[]> {
    return this.http.get<StockTradingGridEntry[]>('/api/stocks/tradingentries')
  }

  getTransactions(ticker:string, groupBy:string, filter:string, txType:string): Observable<TransactionList> {
    if (ticker === null) {
      ticker = ''
    }
    return this.http.get<TransactionList>(`/api/portfolio/transactions?ticker=${ticker}&groupBy=${groupBy}&show=${filter}&txType=${txType}`)
  }

  sendEmail(obj: { to: string; from: string; subject: string; body: string; }) {
    return this.http.post<object>('/api/admin/email', obj)
  }

  weeklyReview(obj:object) {
    return this.http.post('/api/admin/weekly', obj)
  }

  getUsers(): Observable<object[]> {
    return this.http.get<object[]>('/api/admin/users')
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

  // ------------------ alerts ------------------
  getAlerts(ticker: string): Observable<object> {
    if (ticker === null)
    {
      ticker = ''
    }

    return this.http.get<object>('/api/alerts/' + ticker)
  }

  addAlert(ticker: string, description: string, value: number): Observable<object> {
    return this.http.post<object>('/api/alerts', {ticker, value, description})
  }

  removeAlert(ticker: string, id: string): Observable<object> {
    return this.http.post<object>('/api/alerts/delete', {ticker, id})
  }
  //

  getCryptos(): Observable<any> {
		return this.http.get<any>('/api/cryptos')
  }
  importCrypto(formData: FormData) {
    return this.http.post('/api/cryptos/import', formData)
  }
  getCryptoDetails(token:string): Observable<CryptoDetails> {
		return this.http.get<CryptoDetails>('/api/cryptos/' + token)
  }
  getCryptoOwnership(token:string): Observable<CryptoOwnership> {
		return this.http.get<CryptoOwnership>('/api/cryptos/' + token + '/ownership')
  }
  deleteCryptoTransaction(token: string, transactionId:string): Observable<object>{
    return this.http.delete(`/api/cryptos/${token}/transactions/${transactionId}`)
  }

  getStocks(): Observable<any> {
		return this.http.get<any>('/api/stocks')
  }

	getStockDetails(symbol:string): Observable<StockDetails> {
		return this.http.get<StockDetails>(`/api/stocks/${symbol}`)
  }

  getStockPrices2y(symbol:string): Observable<Prices> {
		return this.http.get<Prices>(`/api/stocks/${symbol}/prices/2y`)
  }

  getStockGrid(): Observable<StockGridEntry[]> {
		return this.http.get<StockGridEntry[]>('/api/portfolio/grid')
  }

  deleteStocks(id: string): Observable<object> {
    return this.http.delete(`/api/stocks/${id}`)
  }

  getStockOwnership(ticker:string): Observable<StockOwnership> {
		return this.http.get<StockOwnership>(`/api/stocks/${ticker}/ownership`)
  }

  deleteStockTransaction(id: string, transactionId:string): Observable<object>{
    return this.http.delete(`/api/stocks/${id}/transactions/${transactionId}`)
  }

  importStocks(file: any) : Observable<any> {
    return this.http.post('/api/stocks/import', file)
  }

  importTransactions(file: any) : Observable<any> {
    return this.http.post('/api/transactions/import', file)
  }

	purchase(obj:stocktransactioncommand) : Observable<any> {
		return this.http.post('/api/stocks/purchase', obj)
	}

	sell(obj:stocktransactioncommand) : Observable<any> {
		return this.http.post('/api/stocks/sell', obj)
  }

  settings(ticker:string,category:string) : Observable<any> {
    var obj = {ticker, category}
		return this.http.post('/api/stocks/settings', obj)
  }

  search(term: string): Observable<object[]> {
    if (!term.trim()) {
      return of([]);
    }
    return this.http.get<object[]>(`/api/stocks/search/${term}`).pipe(
      tap(_ => console.log(`found stocks matching "${term}"`))
    );
  }

  // ------- portfolio ----------------

  getPortfolio(): Observable<Dashboard> {
		return this.http.get<Dashboard>('/api/portfolio')
  }

  // ------- options ----------------

  getOptions() : Observable<any> {
    return this.http.get('/api/options')
  }

  deleteOption(id: string) {
    return this.http.delete('/api/options/' + id)
  }

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

  getOwnedOptions(ticker:string): Observable<OwnedOption[]> {
    return this.http.get<OwnedOption[]>('/api/options/' + ticker + '/active')
  }

  getOptionChain(ticker:string): Observable<OptionDetail> {
    return this.http.get<OptionDetail>('/api/options/' + ticker + '/chain')
  }

  importOptions(formData: FormData) {
    return this.http.post('/api/options/import', formData)
  }

  expireOption(obj:object) : Observable<any> {
    return this.http.post('/api/options/expire', obj)
  }

  // ---------- accounts ---------

  getProfile() : Observable<AccountStatus> {
    return this.http.get<AccountStatus>('/api/account/status')
  }

  createAccount(obj:object) : Observable<object> {
    return this.http.post<object>('/api/account', obj)
  }

  validateAccount(obj:object) : Observable<object> {
    return this.http.post<object>('/api/account/validate', obj)
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

  clearAccount() : Observable<object> {
    return this.http.post('/api/account/clear', {})
  }

  resetPassword(obj: { id: string; password: string; }) {
    return this.http.post<object>('/api/account/resetpassword', obj)
  }

  sendMessage(obj: { email: string; message: string; }) {
    return this.http.post<object>('/api/account/contact', obj)
  }

  // ------------------- payments ------------------------
  createSubsription(obj: any) : Observable<object> {
    return this.http.post('/api/account/subscribe', obj)
  }

  // -------------------- reports -------------------------
  chainReport() : Observable<Chain> {
    return this.http.get<Chain>('/api/reports/chain')
  }

  recentSells() : Observable<Sells> {
    return this.http.get<Sells>('/api/reports/sells')
  }
}

export interface Sells {
  sells: Sell[]
}

export interface Sell {
}

export interface Chain {
  links: Link[]
}

export interface Link {
  success: boolean
}

export interface ReviewList {
  start: string
  end: string
  stockProfit: number
  optionProfit: number
  plStockTransactions: Transaction[]
  plOptionTransactions: Transaction[]
  stockTransactions: Transaction[]
  optionTransactions: Transaction[]
}

export interface TransactionList {
  credit: number
  debit: number
  transactions: Transaction[]
}

export interface Transaction {
  aggregateId: string
  credit: number
  price: number
  date: string
  dateAsDate: string
  debit: number
  description: string
  eventId: string
  isOption: boolean
  isPL: boolean
  profit: number
  returnPct: number
  ticker: string
}
export interface NoteList {
  tickers: string[]
  notes: object[]
}

export interface Portfolio {
  owned: OwnedStock[]
  openOptions: OwnedOption[]
  alerts: Alert[]
  triggered: Alert[]
}

export class OwnedStock {
  id:string
  price: number
  ticker: string
  owned: number
  invested: number
  cost: number
  equity: number
  profits: number
  profitsPct: number
  daysHeld: number
  daysSinceLastTransaction: number
  description: string
  averageCost: number
  transactions: TransactionList
  category: string;
}

export interface CryptoDetails {
  token: string
  price: number
}

export interface CryptoOwnership {
  quantity: number
  averageCost: number
  transactions: TransactionList
}

export class OwnedCrypto {
  id:string
  price: number
  token: string
  quantity: number
  cost: number
  equity: number
  profits: number
  profitsPct: number
  daysHeld: number
  daysSinceLastTransaction: number
  description: string
  averageCost: number
  transactions: TransactionList
}

export class Alert {}

export class Dashboard {
  openOptionCount: number
  ownedStockCount: number
  ownedCryptoCount: number
  triggeredAlertCount: number
  alertCount: number
}

export class OwnedOption {
  id:string
  currentPrice: Number
  ticker: string
  optionType: string
  strikePrice: number
  numberOfContracts: number
  boughtOrSold: string
  premiumReceived: number
  profit: number
  transactions: TransactionList
}

export class AlertLabelValue {
  label: string
  value: string

  constructor(label: string, value: string) {
    this.label = label;
    this.value = value;
  }
}

export interface StockHistoricalPrice {
  date: string
  close: number
  volume: number
}

export interface SMA {
  values: number[]
  interval: number
  description: string
}

export interface Prices {
  prices: StockHistoricalPrice[]
  sma: SMA[]
}


export interface StockDetails {
  ticker: string
  price: number
  stats: object
  profile : object
	alert: object
}

export interface StockOwnership {
  id: string
  ticker: string
  cost: number
  averageCost: number
  owned: number
  category: string
  transactions: Transaction[]
}

export interface StockGridEntry {
  price: number,
  stats: any,
  ticker: string,
  above50: number,
  above200: number
}

export interface StockTradingGridEntry {
  price: number,
  stats: any,
  ticker: string,
  numberOfShares: number,
  maxNumberOfShares: number,
  averageCost: number,
  profitTarget: number,
  gain: number,
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
  closed: string
  breakEven: number
  risk: number
  volume : number
  boughtOrSold : string
  expiresSoon : boolean
  isExpired: boolean
  profit: number
  strikePriceDiff: number
  currentPrice: number
  isFavorable: boolean
  itmOtmLabel: string
  days: number
  daysHeld: number
  transactions: Transaction[]
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
  email: string
  firstname: string
  lastname: string
  created: string
  verified: boolean
  loggedIn: boolean
  isAdmin: boolean
}

export interface TrackingPreview {
  companyName: string
  issueType: string
  description: string
  website: string

  avg30Volume: number
  open: number
  close: number
  high: number
  low: number
  change: number
  volume: number
  range: number
  volumeIncrease: number
}

export class job {
  date: string
  ticker: string
  amount: number
  price: number
  bought: boolean
  sold: boolean
  id: string
}

export class stocktransactioncommand {
  ticker: string
  numberOfShares: number
  price: number
  date: string
  notes: string
}
