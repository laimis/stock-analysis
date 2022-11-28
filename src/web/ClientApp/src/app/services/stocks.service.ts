import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';

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

export function toggleVisuallyHidden(element:HTMLElement) {
  const className = 'visually-hidden';
  if (element.classList.contains(className)) {
    element.classList.remove(className);
  } else {
    element.classList.add(className);
  }
}

@Injectable({providedIn: 'root'})
export class StocksService {

  constructor(private http: HttpClient) { }

  // ----------------- misc ---------------------
  getEvents(type:string): Observable<object[]> {
    return this.http.get<object[]>('/api/events?entity=' + type)
  }

  getTransactionSummary(period:string): Observable<ReviewList> {
    return this.http.get<ReviewList>('/api/portfolio/transactionsummary?period=' + period)
  }

  getTradingEntries(): Observable<StockTradingPositions> {
    return this.http.get<StockTradingPositions>('/api/stocks/tradingentries')
  }

  getTransactions(ticker:string, groupBy:string, filter:string, txType:string): Observable<TransactionList> {
    if (ticker === null) {
      ticker = ''
    }
    return this.http.get<TransactionList>(`/api/portfolio/transactions?ticker=${ticker}&groupBy=${groupBy}&show=${filter}&txType=${txType}`)
  }

  simulatePosition(ticker: string, positionId: number): Observable<TradingStrategyResults> {
    return this.http.get<TradingStrategyResults>(
      `/api/portfolio/${ticker}/positions/${positionId}/simulate/default`
    )
  }

  simulatePositions(numberOfPositions: number): Observable<TradingStrategyPerformance[]> {
    return this.http.get<TradingStrategyPerformance[]>(
      `/api/portfolio/simulate/trades?numberOfPositions=${numberOfPositions}`
    )
  }

  smsOff(): Observable<any> {
    return this.http.post<any>('/api/alerts/sms/off', {})
  }

  smsOn(): Observable<any> {
    return this.http.post<any>('/api/alerts/sms/on', {})
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

  // ----------------- alerts ---------------------
  getAlerts(): Observable<AlertsContainer> {
    return this.http.get<AlertsContainer>('/api/alerts')
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

  getStocks(): Observable<StockSummary> {
		return this.http.get<any>('/api/stocks')
  }

	getStockDetails(symbol:string): Observable<StockDetails> {
		return this.http.get<StockDetails>(`/api/stocks/${symbol}`)
  }

  getStockPrice(symbol:string): Observable<number> {
    return this.http.get<number>(`/api/stocks/${symbol}/price`)
  }

  getStockQuote(symbol:string): Observable<StockQuote> {
    return this.http.get<StockQuote>(`/api/stocks/${symbol}/quote`)
  }
  
  getStockPrices(symbol:string, numberOfDays:number): Observable<Prices> {
		return this.http.get<Prices>(`/api/stocks/${symbol}/prices?numberOfDays=${numberOfDays}`)
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

  setStopPrice(ticker:string, stopPrice:number): Observable<object> {
    return this.http.post(`/api/stocks/${ticker}/stop`, {stopPrice, ticker})
  }

  deleteStopPrice(ticker:string): Observable<object> {
    return this.http.delete(`/api/stocks/${ticker}/stop`)
  }

  setRiskAmount(ticker:string, riskAmount:number): Observable<object> {
    return this.http.post(`/api/stocks/${ticker}/risk`, {riskAmount, ticker})
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

  brokerageBuy(obj:brokerageordercommand) : Observable<any> {
		return this.http.post('/api/brokerage/buy', obj)
	}

  brokerageSell(obj:brokerageordercommand) : Observable<any> {
		return this.http.post('/api/brokerage/sell', obj)
	}

  brokerageCancelOrder(orderId:string) : Observable<any> {
    return this.http.delete('/api/brokerage/orders/' + orderId)
  }

  settings(ticker:string,category:string) : Observable<any> {
    var obj = {ticker, category}
		return this.http.post('/api/stocks/settings', obj)
  }

  search(term: string): Observable<StockSearchResult[]> {
    if (!term.trim()) {
      return of([])
    }
    return this.http.get<StockSearchResult[]>(`/api/stocks/search/${term}`)
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

  reportOutcomesAllBars(tickers:string[]) : Observable<OutcomesReport> {
    return this.http.post<OutcomesReport>('/api/reports/outcomes?duration=allbars&includeGapAnalysis=true', tickers)
  }

  reportOutcomesSingleBarDaily(tickers:string[]) : Observable<OutcomesReport> {
    return this.http.post<OutcomesReport>('/api/reports/outcomes?duration=singlebar&frequency=daily', tickers)
  }

  reportOutcomesSingleBarWeekly(tickers:string[]) : Observable<OutcomesReport> {
    return this.http.post<OutcomesReport>('/api/reports/outcomes?duration=singlebar&frequency=weekly', tickers)
  }

  reportTickerPercentChangeDistribution(ticker:string): Observable<StockPercentChangeResponse> {
    return this.http.get<StockPercentChangeResponse>('/api/reports/percentChangeDistribution/tickers/' + ticker)
  }

  reportTickerGaps(ticker:string): Observable<StockGaps> {
    return this.http.get<StockGaps>('/api/reports/gaps/tickers/' + ticker)
  }

  reportPositions(): Observable<OutcomesReport> {
    return this.http.get<OutcomesReport>('/api/reports/positions')
  }
}

export interface StockAlert {
  when: string
  ticker: string
  description: string
  numberOfShares: number
  triggeredValue: number
  alertType: string
}
export interface PriceMonitor {
  description: string
  thresholdValue: number
  lastSeenValue: number
  ticker: string
  triggeredAlert: StockAlert|null
  alertType: string
}

export interface AlertsContainer {
  monitors: PriceMonitor[]
  recentlyTriggered: StockAlert[]
}

export interface StockAnalysisOutcome {
  type: string
  message: string
  key: string
  value: number
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

export interface StockSearchResult {
  symbol: string
  securityName: string
}

export interface ReviewList {
  start: string
  end: string
  stockProfit: number
  optionProfit: number
  openPositions: PositionInstance[]
  closedPositions: PositionInstance[]
  plOptionTransactions: Transaction[]
  plStockTransactions: Transaction[]
  stockTransactions: Transaction[]
  optionTransactions: Transaction[]
}

export interface StockViolation {
  message: string
  numberOfShares: number
  pricePerShare: number
  ticker: string
}

export interface StockSummary {
  positions: PositionInstance[]
  violations: StockViolation[]
  orders: BrokerageOrder[]
}

export interface TransactionList {
  credit: number
  debit: number
  tickers: string[]
  transactions: Transaction[]
  grouped: TransactionGroup[]
}

export interface TransactionGroup {
  name: string
  transactions: TransactionList
}

export interface Transaction {
  aggregateId: string
  amount: number
  price: number
  date: string
  ageInDays: number
  dateAsDate: string
  description: string
  eventId: string
  isOption: boolean
  isPL: boolean
  ticker: string
}
export interface NoteList {
  tickers: string[]
  notes: Note[]
}

export interface Note {
  relatedToTicker: string
  created: string
  price: Price
  note: string
  id: string
  stats: StockAdvancedStats
}

export interface Price {
  amount: number
}

export interface CryptoDetails {
  token: string
  price: Price
  name: string
}

export interface CryptoOwnership {
  quantity: number
  averageCost: number
  transactions: Transaction[]
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

export class Dashboard {
  openOptionCount: number
  openStockCount: number
  openCryptoCount: number
}

export class OwnedOption {
  id:string
  currentPrice: Number
  ticker: string
  optionType: string
  expirationDate: string
  strikePrice: number
  numberOfContracts: number
  boughtOrSold: string
  premiumReceived: number
  profit: number
  transactions: TransactionList
}

export interface PriceBar {
  dateStr: string
  close: number
  open: number
  high: number
  low: number
  volume: number
}

export interface SMA {
  values: number[]
  interval: number
  description: string
}

export interface Prices {
  prices: PriceBar[]
  sma: SMAContainer
}

export interface SMAContainer {
  sma20: SMA
  sma50: SMA
  sma150: SMA
  sma200: SMA
}

export interface StockAdvancedStats {
  companyName: string
  peRatio: number
  avg10Volume: number
  avg30Volume: number
  week52High: number
  week52Low: number
  week52highDate: number
  week52lowDate: number
  marketCap: number
  debtToEquity: number
  putCallRatio: number
  priceToBook: number
  revenue: number
  grossProfit: number
  profitMargin: number
  totalCash : number
  year5ChangePercent: number
  year1ChangePercent: number
  month3ChangePercent: number
  month1ChangePercent: number
  day50MovingAvg: number
  day200MovingAvg: number
}

export interface StockQuote {
  symbol: string
  description: string
  bidPrice: number
  bidSize: number
  bidId: string
  askPrice: number
  askSize: number
  askId: string
  lastPrice: number
  lastSize: number
  lastId: string
  openPrice: number
  highPrice: number
  lowPrice: number
  closePrice: number
  netChange: number
  totalVolume: number
  quoteTimeInLong: number
  tradeTimeInLong: number
  mark: number
  exchange: string
  exchangeName: string
  volatility: number
  securityStatus: string
  regularMarketLastPrice: number
  regularMarketLastSize: number
}

export interface StockDetails {
  ticker: string
  price: number
  stats: StockAdvancedStats
  profile : StockProfile
}

export interface StockProfile {
  description: string
  sector: string
  industry: string
  companyName: string
  country: string
  employees: number
  website: string
  issueType: string
  securityName: string
  symbol: string
}

export interface StockOwnership {
  id: string
  ticker: string
  price: number
  currentPosition: PositionInstance
  positions: PositionInstance[]
}

export interface TickerOutcomes {
  ticker: string
  outcomes: StockAnalysisOutcome[]
}

export interface OutcomesReport {
  evaluations: AnalysisOutcomeEvaluation[],
  outcomes: TickerOutcomes[],
  gaps: StockGaps[],
  summary: TickerCountPair[]
}

export interface TickerCountPair {
  ticker: string
  count: number
}

export interface AnalysisOutcomeEvaluation {
  name: string
  type: string
  sortColumn: string
  matchingTickers: TickerOutcomes[]
}

export interface PercentChangeFrequency {
  percentChange: number
  frequency: number
}
export interface StockPercentChangeDescriptor {
  mean: number
  median: number
  min: number
  max: number
  stdDev: number
  count: number
  skewness: number
  kurtosis: number
  buckets: PercentChangeFrequency[]
}
export interface StockPercentChangeResponse {
  ticker: string
  recent: StockPercentChangeDescriptor
  allTime: StockPercentChangeDescriptor
}

export interface StockGap {
  type: string
  gapSizePct: number
  percentChange: number
  bar: PriceBar,
  closedQuickly: boolean
  open: boolean
  relativeVolume: number
}
export interface StockGaps {
  ticker: string
  gaps: StockGap[]
}

export interface StockTradingPerformance {
  wins: number,
  losses: number,
  total: number,
  profit: number,
  profitRatio: number,
  returnPctRatio: number,
  avgDaysHeld: number,
  avgWinAmount: number,
  maxWinAmount: number,
  winAvgReturnPct: number,
  winMaxReturnPct: number,
  winAvgDaysHeld: number,
  winPct: number,
  avgLossAmount: number,
  maxLossAmount: number,
  lossAvgReturnPct: number,
  lossMaxReturnPct: number,
  lossAvgDaysHeld: number,
  ev: number,
  avgReturnPct: number,
  rrSum: number,
  rrSumWeighted: number,
}

export interface DataPoint {
  value: number
  label: string
}

export interface DataPointContainer {
  label: string
  data: DataPoint[]
}

export interface StockTradingPerformanceCollection {
  overall: StockTradingPerformance,
  recent: StockTradingPerformance,
  trends: DataPointContainer[]
}

export interface BrokerageOrder {
  orderId: string
  price: number
  type: string
  quantity: number
  status: string
  ticker: string
  date: string
  canBeCancelled: boolean
  canBeRecorded: boolean
  isActive: boolean
}

export interface StockTradingPositions {
  current: PositionInstance[]
  past: PositionInstance[]
  performance: StockTradingPerformanceCollection
  brokerageOrders: BrokerageOrder[]
  violations: StockViolation[]
}

export interface PriceWithDate {
  price: number,
  date: string
}
export interface PositionTransaction {
  numberOfShares: number,
  price: number,
  type: string,
  date: string,
  ageInDays: number
  transactionId: string
}

export interface PositionEvent {
  date: string,
  value: number | null,
  type: string,
  description: string,
}

export interface PositionInstance {
  positionId: number,
  averageBuyCostPerShare: number,
  averageCostPerShare: number,
  averageSaleCostPerShare: number,
  category: string,
  closed: string,
  cost: number,
  daysHeld: number,
  daysSinceLastTransaction: number,
  firstBuyCost: number,
  firstBuyNumberOfShares: number,
  gainPct: number,
  isClosed: boolean,
  isShortTerm: boolean,
  lastTransaction: string,
  notes: string[],
  numberOfShares: number,
  opened: string,
  price: number,
  profit: number,
  riskedAmount: number,
  rr: number,
  rrLevels: number[],
  rrWeighted: number,
  stopPrice: number,
  ticker: string,
  transactions: PositionTransaction[],
  events: PositionEvent[],
  unrealizedGainPct: number,
  unrealizedProfit: number,
  unrealizedRR: number,
  percentToStop: number
}

export interface TradingStrategyResult {
  maxDrawdownPct: number
  maxGainPct: number
  position: PositionInstance
  strategyName: string
}

export interface TradingStrategyResults {
  results: TradingStrategyResult[]
}

export interface TradingStrategyPerformance {
  performance: StockTradingPerformance
  positions: PositionInstance[]
  strategyName: string
}

export class OptionDefinition {
  id: string
  ticker: string
  side: string
  openInterest: number
  strikePrice: number
  expirationDate: string
  optionType: string
  numberOfContracts: number
  bid: number
  ask: number
  spread: number
  perDayPrice: number
  lastUpdated: string
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
  subscriptionLevel: string
  connectedToBrokerage: boolean
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
  stopPrice: number | null
}

export class brokerageordercommand {
  ticker: string
  numberOfShares: number
  price: number
  type: string
  duration: string
}
