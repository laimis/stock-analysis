import {Injectable} from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {Observable, of, throwError} from 'rxjs';
import {BrokerageOptionOrder, OptionPosition} from "./option.service";

@Injectable({providedIn: 'root'})
export class StocksService {

    constructor(private http: HttpClient) {
    }

    // ----------------- misc ---------------------
    getEvents(type: string): Observable<object[]> {
        return this.http.get<object[]>('/api/events?entity=' + type)
    }

    // ----------------- alerts ---------------------
    smsOff(): Observable<any> {
        return this.http.post<any>('/api/alerts/sms/off', {})
    }

    smsOn(): Observable<any> {
        return this.http.post<any>('/api/alerts/sms/on', {})
    }

    scheduleAlertRun(): Observable<any> {
        return this.http.post<any>('/api/alerts/run', {})
    }

    getAlerts(): Observable<AlertsContainer> {
        return this.http.get<AlertsContainer>('/api/alerts')
    }

    getAvailableMonitors(): Observable<Monitor[]> {
        return this.http.get<Monitor[]>('/api/alerts/monitors')
    }

    sendEmail(obj: { to: string; from: string; subject: string; body: string; }) {
        return this.http.post<object>('/api/admin/email', obj)
    }

    weeklyReview(obj: object) {
        return this.http.post('/api/admin/weekly', obj)
    }

    getUsers(): Observable<object[]> {
        return this.http.get<object[]>('/api/admin/users')
    }

    // ----------------- routines ---------------------
    getRoutines(): Observable<Routine[]> {
        return this.http.get<Routine[]>('/api/routines')
    }

    createRoutine(name:string, description:string): Observable<Routine> {
        return this.http.post<Routine>('/api/routines', {name, description})
    }

    updateRoutine(id:string, name:string): Observable<Routine> {
        return this.http.put<Routine>('/api/routines/' + id, {name})
    }

    addRoutineStep(id:string, label:string, url:string): Observable<Routine> {
        return this.http.put<Routine>('/api/routines/' + id + "/steps", {label, url, id})
    }

    updateRoutineStep(id:string, stepIndex: number, label:string, url:string) : Observable<Routine> {
        return this.http.post<Routine>('/api/routines/' + id + '/steps/' + stepIndex, {
            label,
            url,
            id,
            stepIndex
        })
    }

    deleteRoutineStep(id:string, stepIndex: number): Observable<Routine> {
        return this.http.delete<Routine>('/api/routines/' + id + '/steps/' + stepIndex)
    }

    moveRoutineStep(id:string, stepIndex: number, direction: number) {
        return this.http.post<Routine>('/api/routines/' + id + '/steps/' + stepIndex + '/position', {
            direction,
            id,
            stepIndex
        })
    }

    // ----------------- stock lists ---------------------
    getStockLists(): Observable<StockList[]> {
        return this.http.get<StockList[]>('/api/stocks/lists')
    }

    getStockList(id: string): Observable<StockList> {
        return this.http.get<StockList>('/api/stocks/lists/' + id)
    }

    addToStockList(id: string, ticker: string): Observable<StockList> {
        return this.http.put<StockList>('/api/stocks/lists/' + id, {id: id, ticker: ticker})
    }

    removeFromStockList(id: string, ticker: string): Observable<StockList> {
        return this.http.delete<StockList>('/api/stocks/lists/' + id + '/' + ticker)
    }

    createStockList(input): Observable<StockList> {
        return this.http.post<StockList>('/api/stocks/lists', input)
    }

    deleteStockList(id: string): Observable<StockList> {
        return this.http.delete<StockList>('/api/stocks/lists/' + id)
    }

    assignTagToStockList(id: string, tag: string): Observable<StockList> {
        return this.http.put<StockList>('/api/stocks/lists/' + id + '/tags', {id: id, tag: tag})
    }

    removeTagFromStockList(id: string, tag: string): Observable<StockList> {
        return this.http.delete<StockList>('/api/stocks/lists/' + id + '/tags/' + tag)
    }

    updateStockList(id: string, newName: string, description: string): Observable<StockList> {
        return this.http.post<StockList>('/api/stocks/lists/' + id, {
            id: id,
            name: newName,
            description: description
        })
    }

    clearStockList(id: string): Observable<StockList> {
        return this.http.post<StockList>('/api/stocks/lists/' + id + '/clear', {})
    }

    // ----------------- pending positions ---------------------
    getPendingStockPositions(): Observable<PendingStockPosition[]> {
        return this.http.get<PendingStockPosition[]>('/api/stocks/pendingpositions')
    }
    
    createPendingStockPosition(cmd: pendingstockpositioncommand): Observable<PendingStockPosition> {
        return this.http.post<PendingStockPosition>('/api/stocks/pendingpositions', cmd)
    }

    closePendingPosition(id: string, reason: string): Observable<any> {
        const cmd = {positionId: id, reason}
        return this.http.post<any>('/api/stocks/pendingpositions/' + id + '/close', cmd)
    }

    getCryptos(): Observable<any> {
        return this.http.get<any>('/api/cryptos')
    }

    importCrypto(formData: FormData) {
        return this.http.post('/api/cryptos/import', formData)
    }

    getCryptoDetails(token: string): Observable<CryptoDetails> {
        return this.http.get<CryptoDetails>('/api/cryptos/' + token)
    }

    getCryptoOwnership(token: string): Observable<CryptoOwnership> {
        return this.http.get<CryptoOwnership>('/api/cryptos/' + token + '/ownership')
    }

    deleteCryptoTransaction(token: string, transactionId: string): Observable<object> {
        return this.http.delete(`/api/cryptos/${token}/transactions/${transactionId}`)
    }

    getStockDetails(symbol: string): Observable<StockDetails> {
        return this.http.get<StockDetails>(`/api/stocks/${symbol}`)
    }

    getStockSECFilings(symbol: string): Observable<SECFilings> {
        return this.http.get<SECFilings>(`/api/stocks/${symbol}/secfilings`)
    }

    getStockPrice(symbol: string): Observable<number> {
        return this.http.get<number>(`/api/stocks/${symbol}/price`)
    }

    getStockQuote(symbol: string): Observable<StockQuote> {
        return this.http.get<StockQuote>(`/api/stocks/${symbol}/quote`)
    }

    getStockPrices(symbol: string, numberOfDays: number, frequency: PriceFrequency): Observable<Prices> {
        return this.http.get<Prices>(`/api/stocks/${symbol}/prices?numberOfDays=${numberOfDays}&frequency=${frequency}`)
    }

    getStockPricesForDates(symbol: string, frequency: PriceFrequency, start: string, end: string): Observable<Prices> {
        return this.http.get<Prices>(`/api/stocks/${symbol}/prices/${start}/${end}?frequency=${frequency}`)
    }

    importTransactions(file: any): Observable<any> {
        return this.http.post('/api/transactions/import', file)
    }

    search(term: string): Observable<StockSearchResult[]> {
        if (!term.trim()) {
            return of([])
        }
        return this.http.get<StockSearchResult[]>(`/api/stocks/search/${term}`)
    }

    getPortfolioHoldings(): Observable<PortfolioHoldings> {
        return this.http.get<PortfolioHoldings>('/api/portfolio')
    }

    // ------- portfolio ----------------

    importStocks(file: any): Observable<any> {
        return this.http.post('/api/portfolio/stockpositions/import', file)
    }

    // ---------- accounts ---------
    getAccountTransactions(): Observable<AccountTransaction[]> {
        return this.http.get<AccountTransaction[]>('/api/account/transactions')
    }
    
    markTransactionAsApplied(transactionId: string): Observable<any> {
        return this.http.post<any>('/api/account/transactions/' + transactionId + '/applied', {})
    }
    
    getProfile(): Observable<AccountStatus> {
        return this.http.get<AccountStatus>('/api/account/status')
    }

    createAccount(obj: object): Observable<object> {
        return this.http.post<object>('/api/account', obj)
    }

    validateAccount(obj: object): Observable<object> {
        return this.http.post<object>('/api/account/validate', obj)
    }

    loginAccount(obj: object): Observable<AccountStatus> {
        return this.http.post<AccountStatus>('/api/account/login', obj)
    }

    requestPasswordReset(obj: object): Observable<object> {
        return this.http.post<object>('/api/account/requestpasswordreset', obj)
    }

    deleteAccount(obj: object): Observable<object> {
        return this.http.post('/api/account/delete', obj)
    }

    clearAccount(): Observable<object> {
        return this.http.post('/api/account/clear', {})
    }

    resetPassword(obj: { id: string; password: string; }) {
        return this.http.post<object>('/api/account/resetpassword', obj)
    }

    sendMessage(obj: { email: string; message: string; }) {
        return this.http.post<object>('/api/account/contact', obj)
    }

    updateAccountSettings(obj: KeyValuePair): Observable<AccountStatus> {
        return this.http.post<AccountStatus>('/api/account/settings', obj)
    }

    // -------------------- reports -------------------------
    reportsWeeklySummary(period: string): Observable<WeeklyReport> {
        return this.http.get<WeeklyReport>('/api/reports/weeklysummary?period=' + period)
    }
    
    reportPortfolioCorrelations(days:number): Observable<TickerCorrelation[]> {
        return this.http.get<TickerCorrelation[]>('/api/reports/portfolio/correlations?days=' + days)
    }
    
    reportCorrelations(tickers: string[], days:number): Observable<TickerCorrelation[]> {
        return this.http.post<TickerCorrelation[]>('/api/reports/correlations', {tickers, days})
    }
    
    chainReport(): Observable<Chain> {
        return this.http.get<Chain>('/api/reports/chain')
    }

    recentSells(): Observable<Sells> {
        return this.http.get<Sells>('/api/reports/sells')
    }

    reportOutcomesAllBars(tickers: string[], startDate: string = null, endDate: string = null): Observable<OutcomesReport> {
        return this.http.post<OutcomesReport>(
            '/api/reports/outcomes',
            {
                tickers,
                duration: "AllBars",
                frequency: PriceFrequency.Daily,
                includeGapAnalysis: true,
                startDate,
                endDate
            }
        )
    }

    reportOutcomesSingleBarDaily(
        tickers: string[],
        highlightTitle: string = null,
        highlightTickers: string[] = null,
        endDate: string = null): Observable<OutcomesReport> {
        return this.http.post<OutcomesReport>(
            '/api/reports/outcomes',
            {
                tickers,
                highlightTitle,
                highlightTickers,
                duration: "SingleBar",
                frequency: PriceFrequency.Daily,
                endDate
            }
        )
    }

    reportOutcomesSingleBarWeekly(tickers: string[], endDate: string = null): Observable<OutcomesReport> {
        return this.http.post<OutcomesReport>(
            '/api/reports/outcomes',
            {
                tickers,
                duration: "SingleBar",
                frequency: PriceFrequency.Weekly,
                endDate
            })
    }

    reportTickerPercentChangeDistribution(ticker: string): Observable<StockPercentChangeResponse> {
        return this.http.get<StockPercentChangeResponse>('/api/reports/percentChangeDistribution/tickers/' + ticker)
    }

    reportTickerGaps(ticker: string, frequency: PriceFrequency = PriceFrequency.Daily): Observable<StockGaps> {
        return this.http.get<StockGaps>('/api/reports/gaps/tickers/' + ticker + '?frequency=' + frequency)
    }

    reportPositions(): Observable<OutcomesReport> {
        return this.http.get<OutcomesReport>('/api/reports/positions')
    }
    
    reportPendingPositions(): Observable<OutcomesReport> {
        return this.http.get<OutcomesReport>('/api/reports/pendingpositions')
    }

    reportDailyPositionReport(positionId: string) {
        var endpoint = '/api/reports/dailypositionreport/' + positionId
        return this.http.get<DailyPositionReport>(endpoint)
    }
    
    reportDailyTickerReport(ticker: string, startDate: string|null = null, endDate: string|null = null): Observable<DailyPositionReport> {
        let endpoint = '/api/reports/dailytickerreport/' + ticker + '?1=1';
        if (startDate) {
            endpoint += '&startDate=' + startDate
        }
        if (endDate) {
            endpoint += '&endDate=' + endDate
        }
        return this.http.get<DailyPositionReport>(endpoint)
    }

    reportDailyOutcomesReport(
        ticker: string,
        start: string,
        end: string = null): Observable<DailyOutcomeScoresReport> {
        var endpoint = '/api/reports/dailyoutcomescoresreport/' + ticker

        if (start) {
            endpoint += '?start=' + start
        }

        if (end) {
            endpoint += '&end=' + end
        }

        return this.http.get<DailyOutcomeScoresReport>(endpoint)
    }
    
    reportInflectionPoints(
        ticker: string,
        start: string,
        end: string = null): Observable<{
            obvTrendAnalysis: {
                establishedTrend: TrendAnalysisResult,
                potentialChange: TrendChangeAlert
            },
            obvInflectionPoints: InflectionPoint[],
            priceTrendAnalysis: {
                establishedTrend: TrendAnalysisResult,
                potentialChange: TrendChangeAlert
            },
            priceInflectionPoints: InflectionPoint[],
            prices: PriceBar[]
            obv: PriceBar[]
        }> {
    
        var endpoint = '/api/reports/inflectionpoints/' + ticker + '?start=' + start
        if (end) {
            endpoint += '&end=' + end
        }

        return this.http.get<any>(endpoint)
    }

    reportTrends(
        ticker: string,
        trendType: TrendType,
        start: string,
        end: string = null): Observable<Trends> {
        var endpoint = '/api/reports/trends/' + ticker + '?start=' + start
        if (end) {
            endpoint += '&end=' + end
        }
        endpoint += '&trendType=' + trendType

        return this.http.get<Trends>(endpoint)
    }

    reportTransactions(ticker: string, groupBy: string, filter: string, txType: string): Observable<TransactionsView> {
        if (ticker === null) {
            ticker = ''
        }
        return this.http.get<TransactionsView>(`/api/reports/transactions?ticker=${ticker}&groupBy=${groupBy}&show=${filter}&txType=${txType}`)
    }

    private handleError(error: HttpErrorResponse) {
        if (error.status === 0) {
            // A client-side or network error occurred. Handle it accordingly.
            console.error('An error occurred:', error.error);
        } else {
            // The backend returned an unsuccessful response code.
            // The response body may contain clues as to what went wrong.
            console.error(
                `Backend returned code ${error.status}, body was: `, error.error);
        }
        // Return an observable with a user-facing error message.
        return throwError(() => new Error('Something bad happened; please try again later.'));
    }

}

export interface StockListTicker {
    ticker: string
    note: string
    when: string
}

export interface RoutineStep {
    label: string
    url?: string
}

export interface Routine {
    id: string
    name: string
    description?: string
    steps: RoutineStep[]
}

export interface StockList {
    id: string
    name: string
    description: string
    tickers: StockListTicker[]
    tags: string[]
}

export interface StockAlert {
    when: string
    ticker: string
    description: string
    identifier: string
    sourceLists: string[]
    triggeredValue: number
    watchedValue: number
    alertType: string
    valueFormat: string
}

export interface StockAlertMessage {
    when: string
    message: string
}

export interface AlertsContainer {
    alerts: StockAlert[]
    recentlyTriggered: StockAlert[]
    messages: StockAlertMessage[]
}

export interface Monitor {
    name: string
    tag: string
}

export enum OutcomeKeys {
    AverageTrueRange = 'AverageTrueRange',
}

export enum OutcomeValueTypeEnum {
    Percentage = 'Percentage',
    Currency = 'Currency',
    Number = 'Number',
    Boolean = 'Boolean',
    String = 'String'
}

export enum ChartType {
    Column = "Column",
    Line = "Line",
    Scatter = "Scatter"
}

export enum OutcomeTypeEnum {
    Positive = "Positive",
    Negative = "Negative",
    Neutral = "Neutral"
}

export enum PriceFrequency {
    Daily = "Daily",
    Weekly = "Weekly",
    Monthly = "Monthly"
}

export interface StockAnalysisOutcome {
    outcomeType: OutcomeTypeEnum
    message: string
    key: string
    value: number
    valueType: OutcomeValueTypeEnum
}

export interface Sells {
    sells: Sell[]
}

export interface Sell {
}

export interface Chain {
    links: Link[]
}

export interface TickerCorrelation {
    ticker: string
    averageCorrelation: number
    correlations: number[]
}

export interface SimulationNotices {
    [key: string]: string[];
}

export interface Link {
    success: boolean
    level: number
    profit: number
    date: string
    ticker: string
}

export interface StockSearchResult {
    symbol: string
    securityName: string
    assetType: string
}

export interface WeeklyReport {
    start: string
    end: string
    stockProfit: number
    optionProfit: number
    totalProfit: number
    openedStocks: StockPosition[]
    closedStocks: StockPosition[]
    openedOptions: OptionPosition[]
    closedOptions: OptionPosition[]
    plStockTransactions: StockPLTransaction[]
    stockTransactions: Transaction[]
    optionTransactions: Transaction[]
    dividends: StockDividendTransaction[]
    fees: StockFeeTransaction[]
}

export class StockViolation {
    message: string
    numberOfShares: number
    pricePerShare: number
    ticker: string
    currentPrice: number
    localPosition: StockPosition
    pendingPosition: PendingStockPosition
}

export interface TransactionsView {
    tickers: string[]
    transactions: Transaction[]
    grouped: TransactionGroup[]
    plBreakdowns: DataPointContainer[]
}

export interface TransactionGroup {
    name: string
    transactions: Transaction[]
    sum: number
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

export interface StockTransaction {
    date: string,
    description: string,
    numberOfShares: number,
    price: number,
    transactionId: string
    type: string
}

export interface StockPLTransaction {
    ticker: string
    date: string
    numberOfShares: number
    buyPrice: number
    sellPrice: number
    profit: number
    gainPct: number
}

export interface StockDividendTransaction {
    ticker: string
    date: string
    netAmount: number
    description: string
}

export interface StockFeeTransaction {
    ticker: string
    date: string
    netAmount: number
    description: string
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
    id: string
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
    transactions: Transaction[]
}

export class PortfolioHoldings {
    stocks: StockPosition[]
    options: OptionPosition[]
    cryptos: OwnedCrypto[]
}

export interface PriceBar {
    dateStr: string
    close: number
    open: number
    high: number
    low: number
    volume: number
}

export interface MovingAverages {
    values: number[]
    interval: number
    exponential: boolean
}

export interface DistributionStatistics {
    buckets: ValueWithFrequency[]
    count: number
    kurtosis: number
    max: number
    mean: number
    median: number
    min: number
    skewness: number
    stdDev: number
}

export interface Prices {
    prices: PriceBar[]
    movingAverages: MovingAveragesContainer
    atr: DataPointContainer
    atrPercent: DataPointContainer
    percentChanges: DistributionStatistics
}

export interface ChartMarker {
    date: string
    color: string
    label: string
    shape: "arrowUp" | "arrowDown"
}

export interface PositionChartInformation {
    ticker: string
    prices: PriceBar[]
    averageBuyPrice: number | null
    stopPrice: number | null
    transactions: StockTransaction[]
    markers: ChartMarker[]
    buyOrders: number[]
    sellOrders: number[]
    movingAverages: MovingAveragesContainer | null
}

export interface MovingAveragesContainer {
    ema20: MovingAverages
    sma20: MovingAverages
    sma50: MovingAverages
    sma150: MovingAverages
    sma200: MovingAverages
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
    price: number
}

export interface StockDetails {
    ticker: string
    quote: StockQuote
    profile: StockProfile
}

export interface SECFiling {
    description: string
    documentsUrl: string
    filingDate: string
    isNew: boolean
    filing: string
    interactiveDataUrl: string
}

export interface SECFilings {
    ticker: string
    filings: SECFiling[]
}

export interface StockProfile {
    description: string
    issueType: string
    securityName: string
    symbol: string
    exchange: string
    fundamentals: Map<string, string>
}

export interface StockOwnership {
    positions: StockPosition[]
}

export interface TickerOutcomes {
    ticker: string
    outcomes: StockAnalysisOutcome[]
}

export interface TickerPatterns {
    ticker: string
    patterns: Pattern[]
}

export interface EvaluationCountPair {
    evaluation: string
    count: number
    type: string
}

export interface Pattern {
    name: string
    date: string
    description: string
    sentimentType: string
}

export interface OutcomesReport {
    evaluations: AnalysisOutcomeEvaluation[],
    outcomes: TickerOutcomes[],
    gaps: StockGaps[],
    tickerSummary: TickerCountPair[],
    evaluationSummary: EvaluationCountPair[],
    patterns: TickerPatterns[],
    failed: string[]
}
export interface DailyOutcomeScoresReport {
    ticker: string
    dailyScores: DataPointContainer
}

export interface DailyPositionReport {
    dailyProfit: DataPointContainer
    dailyGainPct: DataPointContainer
    dailyObv: DataPointContainer
    dailyClose: DataPointContainer
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

export interface ValueWithFrequency {
    value: number
    frequency: number
}

export interface LabelWithFrequency {
    label: string
    frequency: number
}

export interface StockPercentChangeResponse {
    ticker: string
    recent: DistributionStatistics
    allTime: DistributionStatistics
}

export interface StockGap {
    type: string
    gapSizePct: number
    percentChange: number
    closingRange: number
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
    name: string,
    wins: number,
    losses: number,
    numberOfTrades: number,
    profit: number,
    profitRatio: number,
    returnPctRatio: number,
    averageDaysHeld: number,
    avgWinAmount: number,
    maxWinAmount: number,
    winAvgRR: number,
    winAvgReturnPct: number,
    winMaxReturnPct: number,
    winAvgDaysHeld: number,
    winPct: number,
    avgLossAmount: number,
    maxLossAmount: number,
    lossAvgRR: number,
    lossAvgReturnPct: number,
    lossMaxReturnPct: number,
    lossAvgDaysHeld: number,
    ev: number,
    avgReturnPct: number,
    rrSum: number,
    rrRatio: number,
    averageRR: number,
    avgCost: number,
    sharpeRatio:number,
    gainPctStdDev:number,
    earliestDate: string,
    latestDate: string,
    maxCashNeeded: number,
    maxDrawdown: number,
    riskAdjustedReturn: number,
    gradeDistribution: LabelWithFrequency[]
}

export interface DataPoint {
    value: number
    label: string
    isDate: boolean
    ticker?: string
}

export enum ChartAnnotationLineType {
    Horizontal = "Horizontal",
    Vertical = "Vertical"
}

export interface ChartAnnotationLine {
    value: number
    label: string
    chartAnnotationLineType: ChartAnnotationLineType
}

export interface DataPointContainer {
    label: string
    chartType: ChartType
    data: DataPoint[],
    annotationLine?: ChartAnnotationLine
    includeZero?: boolean
}

export interface StockTradingPerformanceCollection {
    performances: StockTradingPerformance[]
    trends: DataPointContainer[][]
}

export interface BrokerageStockOrder {
    orderId: string
    price: number
    type: string
    quantity: number
    status: string
    statusDescription: string
    instruction: string
    ticker: string
    description: string
    executionTime: string
    enteredTime: string
    canBeCancelled: boolean
    canBeRecorded: boolean
    isActive: boolean
    isBuyOrder: boolean
    isSellOrder: boolean
    isCancelledOrRejected: boolean
}

export interface BrokerageStockPositionInstance {
    ticker: string
    quantity: number
    averageCost: number
}

export interface BrokerageAccount {
    stockPositions: BrokerageStockPositionInstance[]
    stockOrders: BrokerageStockOrder[]
    optionOrders: BrokerageOptionOrder[]
    cashBalance: number
    equity: number
    longMarketValue: number
    shortMarketValue: number
    connected: boolean
}

export interface BrokerageAccountSnapshot {
    cash: number
    equity: number
    longValue: number
    shortValue: number
    date: string
}

export interface StockTradingPositions {
    current: StockPosition[]
    violations: StockViolation[]
    brokerageAccount: BrokerageAccount
    prices: Map<string, StockQuote>
    dailyBalances: BrokerageAccountSnapshot[]
}

export interface PastStockTradingPositions {
    past: StockPosition[]
}

export interface PastStockTradingPerformance {
    performance: StockTradingPerformanceCollection
    strategyPerformance: TradingStrategyPerformance[]
}

export interface PriceWithDate {
    price: number,
    date: string
}

export interface PositionEvent {
    id: string,
    date: string,
    value: number | null,
    type: string,
    description: string,
    quantity: number | null
}

export interface PendingStockPosition {
    id: string,
    ticker: string,
    bid: number,
    price: number|null,
    numberOfShares: number,
    stopPrice: number,
    created: string
    numberOfDaysActive: number
    stopLossAmount: number
    notes: string
    strategy: string
}

export interface KeyValuePair {
    key: string,
    value: string
}

export interface Note {
    id: string
    content: string
    created: string
}

export interface StockPosition {
    positionId: string,
    isOpen: boolean,
    averageBuyCostPerShare: number,
    averageCostPerShare: number,
    averageSaleCostPerShare: number,
    closed: string,
    cost: number,
    completedPositionShares: number,
    completedPositionCostPerShare: number,
    daysHeld: number,
    daysSinceLastTransaction: number,
    firstBuyCost: number,
    firstBuyNumberOfShares: number,
    gainPct: number,
    isClosed: boolean,
    lastTransaction: string,
    notes: Note[],
    labels: KeyValuePair[],
    isShort: boolean,
    numberOfShares: number,
    opened: string,
    profit: number,
    profitWithoutDividendsAndFees: number,
    riskedAmount: number,
    rr: number,
    rrWeighted: number,
    stopPrice: number,
    percentToStopFromCost: number,
    ticker: string,
    plTransactions: StockPLTransaction[],
    transactions: StockTransaction[],
    events: PositionEvent[],
    costAtRiskBasedOnStopPrice: number,
    grade: string,
    gradeNote: string
}

export interface TradingStrategyResult {
    maxDrawdownPct: number
    maxDrawdownFirst10Bars: number
    maxGainPct: number
    maxGainFirst10Bars: number
    position: StockPosition
    strategyName: string
    forcedClosed: boolean
}

export interface StrategyProfitPoint {
    name: string
    prices: number[]
}

export interface TradingStrategyResults {
    results: TradingStrategyResult[]
}

export interface TradingStrategyPerformance {
    performance: StockTradingPerformance
    results: TradingStrategyResult[]
    strategyName: string
    numberOfOpenPositions: number
}
export class AccountStatus {
    username: string
    email: string
    firstname: string
    lastname: string
    created: string
    verified: boolean
    loggedIn: boolean
    isAdmin: boolean
    subscriptionLevel: string
    connectedToBrokerage: boolean
    brokerageRefreshTokenExpirationDate: string
    maxLoss: number
    interestReceived: number
    cashTransferred: number
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
    positionId: string
    numberOfShares: number
    price: number
    date: string
    stopPrice: number | null
    brokerageOrderId: string | null
}

export class openpositioncommand {
    ticker: string
    numberOfShares: number
    price: number
    date: string
    notes: string
    stopPrice: number | null
    strategy: string | null
}
export class pendingstockpositioncommand {
    ticker: string
    numberOfShares: number
    price: number
    sizeStopPrice: number
    date: string
    notes: string
    stopPrice: number | null
    strategy: string | null
    useLimitOrder: boolean
}
export enum TrendDirection {
    Up = 'Up',
    Down = 'Down',
}

export enum TrendType {
    Ema20OverSma50 = 'Ema20OverSma50',
    Sma50OverSma200 = 'Sma50OverSma200',
}

export interface Trend {
    ticker: string
    start: PriceBar
    end_: PriceBar
    max: PriceBar
    min: PriceBar
    direction: TrendDirection
    trendType: TrendType
    numberOfDays: number
    numberOfBars: number
    startValue: number
    startDateStr: string
    endValue: number
    endDateStr: string
    gainPercent: number
    maxDate: string
    maxValue: number
    maxAge: number
    minDate: string
    minValue: number
    minAge: number
}

export class Trends {
    ticker: string
    trendType: TrendType
    trends: Trend[]
    upTrends: Trend[]
    downTrends: Trend[]
    currentTrend: Trend
    length: number
    barStatistics: DistributionStatistics
    upBarStatistics: DistributionStatistics
    downBarStatistics: DistributionStatistics
    gainStatistics: DistributionStatistics
    upGainStatistics: DistributionStatistics
    downGainStatistics: DistributionStatistics
    startDateStr: string
    endDateStr: string
    currentTrendRankByBars: number
    currentTrendRankByGain: number
}

export interface AccountTransaction {
    transactionId: string;
    description: string;
    tradeDate: string;
    settlementDate: string;
    netAmount: number;
    brokerageType: string;
    inferredType?: string;
    inferredTicker?: string;
    inserted?: string;
    applied?: string;
}

export enum InfectionPointType { Peak = "Peak", Valley = "Valley" }

export type Gradient = { delta: number, index: number, dataPoint: PriceBar }
export type InflectionPoint = { gradient: Gradient, type: InfectionPointType, priceValue: number }


export enum TrendDirection {
    Uptrend = "Uptrend",
    Downtrend = "Downtrend",
    Sideways = "Sideways",
    InsufficientData = "Insufficient data"
}

export type TrendDirectionAndStrength = {
    direction: TrendDirection;
    strength: number;
}

export type TrendAnalysisDetails = {
    slopeAnalysis: TrendDirectionAndStrength;
    patternAnalysis: TrendDirectionAndStrength;
    rangeAnalysis: TrendDirectionAndStrength;
    strengthAnalysis: TrendDirectionAndStrength;
    percentChangeAnalysis: TrendDirectionAndStrength;
}

export interface TrendAnalysisResult {
    trend: TrendDirection;
    confidence: number;
    details: TrendAnalysisDetails;
}

export interface TrendChangeAlert {
    detected: boolean;
    direction: TrendDirection;
    strength: number; // 0-1 scale
    evidence: string[];
  }
