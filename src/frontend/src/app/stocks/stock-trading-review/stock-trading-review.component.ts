import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
    BrokerageStockOrder,
    PositionChartInformation,
    StockPosition,
    PriceFrequency,
    StocksService,
    TradingStrategyResults,
    StockQuote
} from '../../services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from '../../services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {catchError, tap} from "rxjs/operators";
import {concat} from "rxjs";
import { StockLinkComponent } from "src/app/shared/stocks/stock-link.component";
import { CurrencyPipe } from '@angular/common';
import { ErrorDisplayComponent } from "src/app/shared/error-display/error-display.component";
import { LoadingComponent } from "src/app/shared/loading/loading.component";
import { PriceChartComponent } from "src/app/shared/price-chart/price-chart.component";
import { StockTradingPositionComponent } from "../stock-trading/stock-trading-position.component";
import { TradingActualVsSimulatedPositionComponent } from "../../shared/stocks/trading-actual-vs-simulated.component"
import { StockDailyScoresComponent } from "src/app/shared/stock-daily-scores/stock-daily-scores.component";


@Component({
    selector: 'app-stock-trading-review',
    templateUrl: './stock-trading-review.component.html',
    styleUrls: ['./stock-trading-review.component.css'],
    imports: [
        StockLinkComponent,
        CurrencyPipe,
        ErrorDisplayComponent,
        LoadingComponent,
        PriceChartComponent,
        StockTradingPositionComponent,
        StockDailyScoresComponent,
        TradingActualVsSimulatedPositionComponent
    ]
})
export class StockTradingReviewComponent {
    private stockService = inject(StocksService);
    private stockPositionsService = inject(StockPositionsService);
    private title = inject(Title);


    currentPosition: StockPosition | null = null
    simulationResults: TradingStrategyResults | null = null 
    simulationErrors: string[] = []
    pricesErrors: string[] = []
    positionChartInformation: PositionChartInformation | null = null;
    @Input()
    quotes: Record<string, StockQuote> | null = null
    @Input()
    orders: BrokerageStockOrder[] | null = null
    @Output()
    positionChanged: EventEmitter<any> = new EventEmitter()
    private _index: number = 0

    private _positions: StockPosition[] = [];

    get positions(): StockPosition[] {
        return this._positions
    }

    @Input()
    set positions(value: StockPosition[]) {
        this._index = 0
        this._positions = value
        this.updateCurrentPosition()
    }

    updateCurrentPosition() {
        this.currentPosition = null
        this.simulationResults = null
        this.positionChartInformation = null
        if (this._positions && this._positions.length > 0) {
            const positionId = this._positions[this._index].positionId
            this.loadPositionData(positionId);
        }
    }

    getPrice(position: StockPosition) {
        let quote = this.getQuote(position)

        if (quote) {
            return quote.price
        }

        // check if we have prices perhaps available
        if (this.positionChartInformation && this.positionChartInformation.prices) {
            let prices = this.positionChartInformation.prices
            return prices[prices.length - 1].close
        }

        return 0
    }

    getQuote(position: StockPosition) {
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

    private setTitle(position: StockPosition) {
        this.title.setTitle(`Trading Review - ${position.ticker} - Nightingale Trading`)
    }
    
    private loadPositionData(positionId: string) {
        const loadPositionPromise = this.stockPositionsService.get(positionId)
            .pipe(
                tap(p => {
                    this.currentPosition = p
                    this.setTitle(p)
                    this.loadPrices(p)
                }),
                catchError(e => {
                    this.simulationErrors = GetErrors(e)
                    return []
                })
            )
        
        
        const simulationPromise = this.stockPositionsService.simulatePosition(positionId, true)
            .pipe(
                tap(r => {
                    this.simulationErrors = []
                    this.simulationResults = r
                }),
                catchError(e => {
                    this.simulationErrors = GetErrors(e)
                    return []
                })
            )
        
        concat(loadPositionPromise, simulationPromise).subscribe()
    }
    
    private loadPrices(position: StockPosition) {
        this.stockService.getStockPrices(position.ticker, 365, PriceFrequency.Daily)
            .subscribe(
                r => {
                    this.pricesErrors = []
                    
                    let buyOrders : number[] = []
                    let sellOrders : number[] = []
                    
                    if (this.orders)
                    {
                        buyOrders =
                            this.orders.filter(o => o.isBuyOrder && o.isActive && o.ticker === position.ticker)
                                .map(o => o.price)

                        sellOrders =
                            this.orders.filter(o => !o.isBuyOrder && o.isActive && o.ticker === position.ticker)
                                .map(o => o.price)

                    }
                    // markers should include active orders if orders are present
                    
                    this.positionChartInformation = {
                        averageBuyPrice: position.averageCostPerShare,
                        stopPrice: position.stopPrice,
                        markers: [],
                        prices: r.prices,
                        ticker: position.ticker,
                        transactions: position.transactions,
                        buyOrders: buyOrders,
                        sellOrders: sellOrders,
                        movingAverages: r.movingAverages
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
