import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
    BrokerageStockOrder,
    ChartMarker,
    DailyPositionReport, DataPointContainer,
    PositionChartInformation,
    PositionInstance,
    PriceFrequency,
    Prices,
    StocksService,
    TradingStrategyResults
} from 'src/app/services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {catchError, tap} from "rxjs/operators";
import {concat} from "rxjs";


@Component({
    selector: 'app-stock-trading-review',
    templateUrl: './stock-trading-review.component.html',
    styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent {

    currentPosition: PositionInstance
    simulationResults: TradingStrategyResults
    simulationErrors: string[];
    scoresErrors: string[];
    pricesErrors: string[]
    dailyPositionReport: DailyPositionReport
    positionChartInformation: PositionChartInformation;
    @Input()
    quotes: object
    @Input()
    orders: BrokerageStockOrder[]
    @Output()
    brokerageOrdersChanged: EventEmitter<string> = new EventEmitter<string>()
    gradingError: string = null
    gradingSuccess: string = null
    assignedGrade: string = null
    assignedNote: string = null
    private _index: number = 0

    constructor(
        private stockService: StocksService,
        private stockPositionsService: StockPositionsService,
        private title: Title) {
    }

    private _positions: PositionInstance[];

    get positions(): PositionInstance[] {
        return this._positions
    }

    @Input()
    set positions(value: PositionInstance[]) {
        this._index = 0
        this._positions = value
        this.updateCurrentPosition()
    }

    updateCurrentPosition() {
        const positionId = this._positions[this._index].positionId
        this.currentPosition = null
        // get price data and pass it to chart
        this.gradingError = null
        this.gradingSuccess = null
        this.simulationResults = null
        this.dailyPositionReport = null
        this.positionChartInformation = null
        this.loadPositionData(positionId);
    }

    getPrice(position: PositionInstance) {
        let quote = this.getQuote(position)

        if (quote) {
            return quote.price
        }

        // check if we have prices perhaps available
        if (this.positionChartInformation && this.positionChartInformation.prices) {
            let prices = this.positionChartInformation.prices
            return prices.prices[prices.prices.length - 1].close
        }

        return 0
    }

    getQuote(position: PositionInstance) {
        if (this.quotes && position.ticker in this.quotes) {
            return this.quotes[position.ticker]
        }
        return null
    }

    dropdownClick(elem: EventTarget) {
        this._index = Number.parseInt((elem as HTMLInputElement).value)
        this.updateCurrentPosition()
    }

    next() {
        this._index++
        if (this._index >= this.positions.length) {
            this._index = 0
        }
        this.updateCurrentPosition()
    }

    previous() {
        this._index--
        if (this._index < 0) {
            this._index = 0
        }
        this.updateCurrentPosition()
    }

    assignGrade(note: string) {
        this.assignedNote = note
        this.stockPositionsService.assignGrade(
            this.currentPosition.positionId,
            this.assignedGrade,
            note).subscribe(
            (_: any) => {
                this.gradingSuccess = "Grade assigned successfully"
                setTimeout(() => {
                    this.gradingSuccess = null
                }, 5000)
            },
            (error) => {
                let errors = GetErrors(error)
                this.gradingError = errors.join(', ')
            }
        );
    }

    private setTitle(position: PositionInstance) {
        this.title.setTitle(`Trading Review - ${position.ticker} - Nightingale Trading`)
    }
    
    private loadPositionData(positionId: string) {
        const loadPositionPromise = this.stockPositionsService.get(positionId)
            .pipe(
                tap(p => {
                    this.currentPosition = p
                    this.assignedGrade = this.currentPosition.grade
                    this.assignedNote = this.currentPosition.gradeNote
                    this.setTitle(p)
                    this.loadPrices(p)
                }),
                catchError(e => {
                    this.gradingError = GetErrors(e).join(', ')
                    return []
                })
            )
        
        
        const simulationPromise = this.stockPositionsService.simulatePosition(positionId, true)
            .pipe(
                tap(r => {
                    this.simulationErrors = null
                    this.simulationResults = r
                }),
                catchError(e => {
                    this.simulationErrors = GetErrors(e)
                    return []
                })
            )
        
        const reportPromise = this.stockService.reportDailyPositionReport(positionId)
            .pipe(
                tap(r => {
                    this.scoresErrors = null
                    this.dailyPositionReport = r
                }),
                catchError(e => {
                    this.scoresErrors = GetErrors(e)
                    return []
                })
            )
        
        concat(loadPositionPromise, simulationPromise, reportPromise).subscribe()
    }
    
    private loadPrices(position: PositionInstance) {
        this.stockService.getStockPrices(position.ticker, 365, PriceFrequency.Daily)
            .subscribe(
                r => {
                    this.pricesErrors = null
                    this.positionChartInformation = {
                        averageBuyPrice: position.averageCostPerShare,
                        stopPrice: position.stopPrice,
                        markers: [],
                        prices: r,
                        ticker: position.ticker,
                        transactions: position.transactions
                    }
                },
                e => {
                    this.pricesErrors = GetErrors(e)
                    return []
                }
            )
    }

    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;
}
