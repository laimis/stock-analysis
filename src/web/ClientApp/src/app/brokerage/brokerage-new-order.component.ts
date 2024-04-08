import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Observable} from 'rxjs';
import {brokerageordercommand, KeyValuePair, StockQuote, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from '../services/utils';
import {BrokerageOrderDuration, BrokerageOrderType, BrokerageService} from "../services/brokerage.service";
import {StockPositionsService} from "../services/stockpositions.service";


@Component({
    selector: 'app-brokerage-new-order',
    templateUrl: './brokerage-new-order.component.html',
    styleUrls: ['./brokerage-new-order.component.css']
})
export class BrokerageNewOrderComponent {

    brokerageOrderDuration: string
    brokerageOrderType: string
    numberOfShares: number | null = null
    price: number | null = null
    notes: string | null = null
    selectedTicker: string
    quote: StockQuote
    total: number | null = null
    errors: string[] | null = null;

    submittedOrder = false
    submittingOrder = false
    orderDurations: KeyValuePair[]
    @Output()
    brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()
    private marketOrderTypes: KeyValuePair[] = [
        {key: BrokerageOrderDuration.Day, value: 'Day'},
        {key: BrokerageOrderDuration.Gtc, value: 'GTC'}
    ]
    private nonMarketOrderTypes: KeyValuePair[] = [
        {key: BrokerageOrderDuration.Day, value: 'Day'},
        {key: BrokerageOrderDuration.Gtc, value: 'GTC'},
        {key: BrokerageOrderDuration.DayPlus, value: 'Day+AH'},
        {key: BrokerageOrderDuration.GtcPlus, value: 'GTC+AH'}
    ]

    constructor(
        private brokerage: BrokerageService,
        private stockService: StocksService,
        private stockPositions: StockPositionsService
    ) {
        this.brokerageOrderType = BrokerageOrderType.Limit
        this.brokerageOrderTypeChanged()
    }

    @Input()
    set ticker(value: string) {
        this.selectTicker(value)
    }
    
    @Input()
    positionId: string

    numberOfSharesChanged() {
        if (this.numberOfShares && this.price) {
            this.total = this.numberOfShares * this.price
        } else {
            this.total = null
        }
    }

    brokerageOrderTypeChanged() {
        if (this.brokerageOrderType === BrokerageOrderType.Market) {
            this.orderDurations = this.marketOrderTypes
            this.brokerageOrderDuration = BrokerageOrderDuration.Gtc
        } else {
            this.orderDurations = this.nonMarketOrderTypes
            this.brokerageOrderDuration = BrokerageOrderDuration.GtcPlus
        }
    }

    reset() {
        this.numberOfShares = null
        this.errors = null
    }

    onTickerSelected(ticker: string) {
        this.selectTicker(ticker)
    }

    selectTicker(ticker: string) {
        this.selectedTicker = ticker
        this.stockService.getStockQuote(ticker).subscribe(
            prices => {
                this.quote = prices
                this.price = prices.mark

                this.stockPositions.getStockOwnership(ticker).subscribe(
                    ownership => {
                        let position = ownership.positions.filter(p => p.isOpen)[0]
                        if (position) {
                            this.numberOfShares = position.numberOfShares
                        }
                    }
                )
            }
        )
    }

    buy() {
        this.execute(cmd => this.brokerage.buy(cmd))
    }

    sell() {
        this.execute(cmd => this.brokerage.sell(cmd))
    }

    buyToCover() {
        this.execute(cmd => this.brokerage.buyToCover(cmd))
    }

    sellShort() {
        this.execute(cmd => this.brokerage.sellShort(cmd))
    }

    execute(fn: (cmd: brokerageordercommand) => Observable<string>) {
        this.errors = null
        this.submittingOrder = true
        const cmd: brokerageordercommand = {
            ticker: this.selectedTicker,
            numberOfShares: this.numberOfShares,
            price: this.price,
            type: this.brokerageOrderType,
            duration: this.brokerageOrderDuration,
            positionId: this.positionId,
            notes: this.notes
        }

        fn(cmd).subscribe(
            _ => {
                this.submittingOrder = false
                this.submittedOrder = true
                this.reset()
                this.brokerageOrderEntered.emit(this.selectedTicker)
            },
            err => {
                this.submittingOrder = false
                this.submittedOrder = true
                this.errors = GetErrors(err)
                console.error(err)
            }
        )
    }
}
