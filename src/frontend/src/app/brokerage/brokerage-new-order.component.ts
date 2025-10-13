import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import {Observable} from 'rxjs';
import {KeyValuePair, StockQuote, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from '../services/utils';
import {
    BrokerageOrderCommand,
    BrokerageOrderDuration,
    BrokerageOrderType,
    BrokerageService
} from "../services/brokerage.service";
import {StockPositionsService} from "../services/stockpositions.service";
import {FormControl, FormsModule, ReactiveFormsModule, Validators} from "@angular/forms";
import {StockSearchComponent} from "../stocks/stock-search/stock-search.component";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";
import { CurrencyPipe, NgClass } from "@angular/common";


@Component({
    selector: 'app-brokerage-new-order',
    templateUrl: './brokerage-new-order.component.html',
    styleUrls: ['./brokerage-new-order.component.css'],
    imports: [
    FormsModule,
    StockSearchComponent,
    ErrorDisplayComponent,
    NgClass,
    ReactiveFormsModule,
    CurrencyPipe
]
})
export class BrokerageNewOrderComponent {
    private brokerage = inject(BrokerageService);
    private stockService = inject(StocksService);
    private stockPositions = inject(StockPositionsService);


    brokerageOrderDuration: string
    brokerageOrderType: string
    numberOfShares: number | null = null
    price: number | null = null
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
        {key: BrokerageOrderDuration.GTC, value: 'GTC'}
    ]
    private nonMarketOrderTypes: KeyValuePair[] = [
        {key: BrokerageOrderDuration.Day, value: 'Day'},
        {key: BrokerageOrderDuration.GTC, value: 'GTC'},
        {key: BrokerageOrderDuration.DayPlus, value: 'Day+AH'},
        {key: BrokerageOrderDuration.GtcPlus, value: 'GTC+AH'}
    ]

    notesControl = new FormControl('')

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
        this.brokerageOrderType = BrokerageOrderType.Limit
        this.brokerageOrderTypeChanged()
    }

    @Input()
    set ticker(value: string) {
        this.selectTicker(value)
    }
    
    private _positionId: string = null
    @Input()
    set positionId(value:string) {
        this._positionId = value
        if (value) {
            this.notesControl.setValidators(Validators.required)
        } else {
            this.notesControl.clearValidators()
        }
        this.notesControl.updateValueAndValidity()
    }
    get positionId() {
        return this._positionId
    }

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
            this.brokerageOrderDuration = BrokerageOrderDuration.GTC
        } else {
            this.orderDurations = this.nonMarketOrderTypes
            this.brokerageOrderDuration = BrokerageOrderDuration.GtcPlus
        }
    }

    reset() {
        this.numberOfShares = null
        this.errors = null
        this.selectedTicker = null
        this.positionId = null
        this.quote = null
        this.price = null
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
                            this.positionId = position.positionId
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

    execute(fn: (cmd: BrokerageOrderCommand) => Observable<string>) {
        this.errors = null
        
        if (this.notesControl.invalid) {
            if (this.notesControl.errors.required) {
                this.errors = ['Notes field is required.'];    
            } else {
                this.errors = ['Notes field is invalid.'];
            }
            return;
        }
        
        this.submittingOrder = true
        const cmd: BrokerageOrderCommand = {
            ticker: this.selectedTicker,
            numberOfShares: this.numberOfShares,
            price: this.price,
            type: this.brokerageOrderType,
            duration: this.brokerageOrderDuration,
            positionId: this.positionId,
            notes: this.notesControl.value
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
            }
        )
    }
}
