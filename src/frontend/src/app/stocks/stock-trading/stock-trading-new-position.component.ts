import { CurrencyPipe, DatePipe, NgClass, PercentPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import {
    MovingAverages,
    openpositioncommand,
    pendingstockpositioncommand,
    PriceFrequency,
    Prices,
    StocksService
} from 'src/app/services/stocks.service';
import {GetErrors, GetStockStrategies} from 'src/app/services/utils';
import {GlobalService} from "../../services/global.service";
import {StockPositionsService} from "../../services/stockpositions.service";
import {StockSearchComponent} from "../stock-search/stock-search.component";
import {FormsModule} from "@angular/forms";
import {StockLinkComponent} from "../../shared/stocks/stock-link.component";
import {TradingViewLinkComponent} from "../../shared/stocks/trading-view-link.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {StockAnalysisComponent} from "../stock-details/stock-analysis.component";

@Component({
    selector: 'app-stock-trading-new-position',
    templateUrl: './stock-trading-new-position.component.html',
    styleUrls: ['./stock-trading-new-position.component.css'],
    providers: [DatePipe],
    imports: [
    CurrencyPipe,
    StockSearchComponent,
    FormsModule,
    PercentPipe,
    StockLinkComponent,
    TradingViewLinkComponent,
    NgClass,
    StockAnalysisComponent,
    ErrorDisplayComponent
]
})
export class StockTradingNewPositionComponent {
    private stockService = inject(StocksService);
    private stockPositionsService = inject(StockPositionsService);

    strategies: { key: string; value: string; }[];
    prices: Prices;
    maxLoss = 60;
    @Input()
    showAnalysis: boolean = true
    @Input()
    recordPositions: boolean = true
    @Input()
    presetTicker: string
    @Input()
    price: number | null = null
    @Input()
    numberOfShares: number | null = null
    @Input()
    isPendingPositionMode: boolean = true
    positionEntered = false
    pendingPositionEntered = false
    workflowStarted = false
    recordInProgress: boolean = false
    @Output()
    positionOpened: EventEmitter<openpositioncommand> = new EventEmitter<openpositioncommand>()
    @Output()
    pendingPositionCreated: EventEmitter<pendingstockpositioncommand> = new EventEmitter<pendingstockpositioncommand>()
    @Output()
    newPositionWorkflowStarted: EventEmitter<void> = new EventEmitter<void>()
    
    // variables for new positions
    positionSizeCalculated: number = null
    ask: number | null = null
    bid: number | null = null
    sizeStopPrice: number | null = null
    positionStopPrice: number | null = null
    oneR: number | null = null
    potentialLoss: number | null = null
    stopPct: number | null = null
    date: string | null = null
    ticker: string | null = null
    notes: string | null = null
    strategy: string = ""
    chartStop: number = null;
    errors: string[] = []
    protected readonly atrMultiplier = 2;

    constructor() {
        const globalService = inject(GlobalService);

        this.strategies = GetStockStrategies()
        globalService.accountStatusFeed.subscribe(value => {
            if (value.maxLoss) {
                this.maxLoss = value.maxLoss
            }
        }, error => {
            this.errors = GetErrors(error)
        })
    }

    startNewPosition() {
        this.positionEntered = false
        this.workflowStarted = true
        this.newPositionWorkflowStarted.emit()
        if (this.presetTicker) {
            this.onBuyTickerSelected(this.presetTicker)
        }
    }

    onBuyTickerSelected(ticker: string) {

        this.sizeStopPrice = null
        this.positionStopPrice = null
        this.ticker = ticker

        this.stockService.getStockQuote(ticker)
            .subscribe(quote => {
                    if (!this.price) {
                        this.price = quote.mark
                    }
                    this.ask = quote.askPrice
                    this.bid = quote.bidPrice
                    this.fetchPrices(ticker)
                    this.lookupPendingPosition(ticker)
                }, error => {
                    this.errors = GetErrors(error)
                }
            );
    }

    reset() {
        this.price = null
        this.numberOfShares = null
        this.positionStopPrice = null
        this.sizeStopPrice = null
        this.ticker = null
        this.prices = null
        this.notes = null
        this.strategy = ""
        this.workflowStarted = false
    }

    fetchPrices(ticker: string) {
        this.stockService.getStockPrices(ticker, 365, PriceFrequency.Daily).subscribe(
            prices => {
                this.prices = prices
            }
        )
    }

    get20sma(): number {
        return this.getLastValue(this.prices.movingAverages.sma20)
    }

    smaCheck20(): boolean {
        return this.get20sma() < this.price && this.get20sma() > this.get50sma()
    }

    get50sma(): number {
        return this.getLastValue(this.prices.movingAverages.sma50)
    }

    smaCheck50(): boolean {
        return this.get50sma() < this.price && this.get50sma() > this.get150sma()
    }

    get150sma(): number {
        return this.getLastValue(this.prices.movingAverages.sma150)
    }

    get200sma(): number {
        return this.getLastValue(this.prices.movingAverages.sma200)
    }

    smaCheck150(): boolean {
        return this.get150sma() < this.price && this.get150sma() < this.get50sma()
    }

    smaCheck200(): boolean {
        return this.get200sma() < this.price && this.get200sma() < this.get150sma()
    }

    getLastValue(sma: MovingAverages): number {
        return sma.values.slice(-1)[0]
    }

    updateBuyingValuesWithNumberOfShares() {
        if (!this.price || !this.positionStopPrice) {
            console.log("not enough info to calculate")
            return
        }
        let singleShareLoss = this.price - this.positionStopPrice
        this.updateBuyingValues(singleShareLoss)
    }

    updateBuyingValuesSizeStopPrice() {
        if (!this.price) {
            console.log("sizeStopChanged: not enough info to calculate the rest")
            return
        }

        if (!this.sizeStopPrice) {
            console.log("sizeStopChanged: no size stop price")
            return
        }
        // right now we don't have a toggle for short or long, so we determine that
        // based on sizeStopPrice, if it's above price, then it's a short
        let singleShareLoss = this.sizeStopPrice > this.price ? this.sizeStopPrice - this.price : this.price - this.sizeStopPrice
        let numberOfShares = Math.floor(this.maxLoss / singleShareLoss)

        this.numberOfShares = this.sizeStopPrice > this.price ? -numberOfShares : numberOfShares

        this.updateBuyingValues(singleShareLoss)
    }

    updateBuyingValuesPositionStopPrice() {
        if (!this.price) {
            console.log("positionStopChanged: not enough info to calculate the rest")
            return
        }
        let singleShareLoss = this.price - this.positionStopPrice
        this.updateBuyingValues(singleShareLoss)
    }

    updateBuyingValuesWithCostToBuy() {
        this.updateBuyingValuesSizeStopPrice()
    }

    updateBuyingValues(singleShareLoss: number) {
        console.log("updateBuyingValues")
        // how many shares can we buy to keep the loss under $100
        let positionStopPrice = this.positionStopPrice
        if (!positionStopPrice) {
            positionStopPrice = this.sizeStopPrice
        }

        this.positionSizeCalculated = Math.round(this.numberOfShares * this.price * 100) / 100
        this.oneR = this.price + singleShareLoss
        this.potentialLoss = positionStopPrice * this.numberOfShares - this.price * this.numberOfShares
        this.stopPct = Math.round((positionStopPrice - this.price) / this.price * 100) / 100
        this.chartStop = positionStopPrice
    }

    openPosition() {
        this.recordInProgress = true
        let cmd = this.createOpenPositionCommand();

        if (!this.recordPositions) {
            this.positionOpened.emit(cmd)
            this.recordInProgress = false
            return
        }

        this.stockPositionsService.openPosition(cmd).subscribe(
            _ => {
                this.positionOpened.emit(cmd)
                this.recordInProgress = false
                this.positionEntered = true
                this.reset()
            },
            err => {
                let errors = GetErrors(err)
                let errorMessages = errors.join(", ")
                alert('purchase failed: ' + errorMessages)
                this.recordInProgress = false
            }
        )
    }

    createPendingPositionLimit() {
        this.createPendingPosition(true)
    }

    createPendingPositionMarket() {
        this.createPendingPosition(false)
    }

    createPendingPosition(useLimitOrder: boolean) {
        let cmd = this.createPendingPositionCommand(useLimitOrder)

        this.stockService.createPendingStockPosition(cmd).subscribe(
            _ => {
                this.ticker = null
                this.prices = null
                this.flashPendingPositionCreated()
                this.pendingPositionCreated.emit(cmd)
                this.reset()
            },
            errors => {
                let errorMessages = GetErrors(errors).join(", ")
                alert('pending position failed: ' + errorMessages)
            }
        )
    }

    flashPendingPositionCreated() {
        this.pendingPositionEntered = true
        setTimeout(() => {
            this.pendingPositionEntered = false
        }, 1000)
    }

    lookupPendingPosition(ticker: string) {
        this.stockService.getPendingStockPositions().subscribe(
            positions => {
                let position = positions.find(p => p.ticker === ticker)
                if (position) {
                    this.price = position.bid
                    this.numberOfShares = null
                    this.positionStopPrice = position.stopPrice
                    this.notes = position.notes
                    this.strategy = position.strategy
                    this.updateBuyingValuesPositionStopPrice()
                }
            },
            errors => {
                this.errors = GetErrors(errors)
            }
        )
    }
    
    calculateAtrBasedStop() {
        if (!this.prices) {
            return
        }
        return this.price - this.prices.atr.data[this.prices.atr.data.length-1].value * this.atrMultiplier
    }

    assignSizeStopPrice(value: number) {

        // round to 2 decimal places
        value = Math.round(value * 100) / 100

        if (this.isPendingPositionMode) {
            this.sizeStopPrice = value
        } else {
            this.positionStopPrice = value
        }
        this.updateBuyingValuesSizeStopPrice()
    }

    private createPendingPositionCommand(useLimitOrder: boolean) {
        let cmd = new pendingstockpositioncommand();
        cmd.ticker = this.ticker;
        cmd.numberOfShares = this.numberOfShares;
        cmd.price = this.price;
        cmd.sizeStopPrice = this.sizeStopPrice;
        cmd.stopPrice = this.positionStopPrice;
        cmd.notes = this.notes;
        cmd.date = this.date;
        cmd.strategy = this.strategy;
        cmd.useLimitOrder = useLimitOrder;
        return cmd;
    }

    private createOpenPositionCommand(): openpositioncommand {
        return {
            numberOfShares: this.numberOfShares,
            price: this.price,
            stopPrice: this.positionStopPrice,
            notes: this.notes,
            date: this.date,
            ticker: this.ticker,
            strategy: this.strategy
        }
    }
}

