import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {
    KeyValuePair,
    openpositioncommand,
    PastStockTradingPerformance,
    PastStockTradingPositions,
    PositionInstance,
    StockOwnership,
    StockTradingPositions,
    stocktransactioncommand,
    StrategyProfitPoint,
    TradingStrategyPerformance,
    TradingStrategyResults
} from "./stocks.service";

@Injectable({providedIn: 'root'})
export class StockPositionsService {

    constructor(private http: HttpClient) {
    }

    getTradingEntries(): Observable<StockTradingPositions> {
        return this.http.get<StockTradingPositions>('/api/portfolio/stockpositions/tradingentries')
    }

    getPastTradingEntries(): Observable<PastStockTradingPositions> {
        return this.http.get<PastStockTradingPositions>('/api/portfolio/stockpositions/pasttradingentries')
    }

    getPastTradingPerformance(): Observable<PastStockTradingPerformance> {
        return this.http.get<PastStockTradingPerformance>('/api/portfolio/stockpositions/pasttradingperformance')
    }

    simulatePosition(positionId: string): Observable<TradingStrategyResults> {
        return this.http.get<TradingStrategyResults>(
            `/api/portfolio/stockpositions/${positionId}/simulate/trades`
        )
    }

    simulatePositions(closePositionIfOpenAtTheEnd: boolean, numberOfTrades: number): Observable<TradingStrategyPerformance[]> {
        return this.http.get<TradingStrategyPerformance[]>(
            `/api/portfolio/stockpositions/simulate/trades?numberOfTrades=${numberOfTrades}&closePositionIfOpenAtTheEnd=${closePositionIfOpenAtTheEnd}`
        )
    }

    simulatePositionsExportUrl(closePositionIfOpenAtTheEnd: boolean, numberOfTrades: number): string {
        return `/api/portfolio/stockpositions/simulate/trades/export?numberOfTrades=${numberOfTrades}&closePositionIfOpenAtTheEnd=${closePositionIfOpenAtTheEnd}`
    }

    getStrategyProfitPoints(positionId: string, numberOfPoints: number): Observable<StrategyProfitPoint[]> {
        return this.http.get<StrategyProfitPoint[]>(
            `/api/portfolio/stockpositions/${positionId}/profitpoints?numberOfPoints=${numberOfPoints}`
        )
    }

    assignGrade(positionId: string, grade: string, gradeNote: string): Observable<any> {
        return this.http.post<any>(
            `/api/portfolio/stockpositions/${positionId}/grade`,
            {grade, gradeNote, positionId}
        )
    }

    openPosition(command: openpositioncommand): Observable<PositionInstance> {
        return this.http.post<PositionInstance>(`/api/portfolio/stockpositions`, command)
    }

    deletePosition(positionId: string): Observable<object> {
        return this.http.delete(`/api/portfolio/stockpositions/${positionId}`)
    }

    closePosition(positionId: string,closeReason: string): Observable<object> {
        return this.http.post(`/api/portfolio/stockpositions/${positionId}/close`, {positionId, closeReason})
    }

    setLabel(positionId: string, label: KeyValuePair): Observable<object> {
        return this.http.post(`/api/portfolio/stockpositions/${positionId}/labels`, {
            key: label.key,
            value: label.value,
            positionId: positionId
        })
    }

    deleteLabel(positionId: string, labelKey: string): Observable<object> {
        return this.http.delete(`/api/portfolio/stockpositions/${positionId}/labels/${labelKey}`)
    }

    getStockOwnership(ticker: string): Observable<StockOwnership> {
        return this.http.get<StockOwnership>(`/api/portfolio/stockpositions/ownership/${ticker}`)
    }

    setRiskAmount(positionId: string, riskAmount: number): Observable<object> {
        return this.http.post(`/api/portfolio/stockpositions/${positionId}/risk`, {riskAmount, positionId})
    }

    purchase(obj: stocktransactioncommand): Observable<any> {
        return this.http.post(`/api/portfolio/stockpositions/${obj.positionId}/buy`, obj)
    }

    sell(obj: stocktransactioncommand): Observable<any> {
        return this.http.post(`/api/portfolio/stockpositions/${obj.positionId}/sell`, obj)
    }

    deleteTransaction(positionId: string, transactionId: string): Observable<object> {
        return this.http.delete(`/api/portfolio/stockpositions/${positionId}/transactions/${transactionId}`)
    }

    setStopPrice(positionId: string, stopPrice: number): Observable<object> {
        return this.http.post(`/api/portfolio/stockpositions/${positionId}/stop`, {stopPrice, positionId})
    }

    deleteStopPrice(positionId: string): Observable<object> {
        return this.http.delete(`/api/portfolio/stockpositions/${positionId}/stop`)
    }


}
