import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import {BrokerageAccount, BrokerageStockOrder, stocktransactioncommand} from 'src/app/services/stocks.service';
import {GetErrors} from '../services/utils';
import {BrokerageService} from "../services/brokerage.service";
import {StockPositionsService} from "../services/stockpositions.service";
import { CurrencyPipe, DatePipe, NgClass } from "@angular/common";
import {StockLinkAndTradingviewLinkComponent} from "../shared/stocks/stock-link-and-tradingview-link.component";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";
import {BrokerageOptionOrder} from "../services/option.service";

let orderBy = (a: BrokerageStockOrder, b: BrokerageStockOrder) => {
    let tickerComparison = a.ticker.localeCompare(b.ticker)
    if (tickerComparison === 0) {
        return a.executionTime > b.executionTime ? -1 : 1
    } else {
        return tickerComparison
    }
}
@Component({
    selector: 'app-brokerage-orders',
    templateUrl: './brokerage-orders.component.html',
    imports: [
    NgClass,
    StockLinkAndTradingviewLinkComponent,
    CurrencyPipe,
    DatePipe,
    ErrorDisplayComponent
]
})
export class BrokerageOrdersComponent {
    private brokerage = inject(BrokerageService);
    private stocks = inject(StockPositionsService);

    groupedOrders: BrokerageStockOrder[][];
    isEmpty: boolean = false;
    errors: string[];
    @Input()
    justOrders: boolean = false;
    @Input()
    positionId: string


    private _stockOrders: BrokerageStockOrder[] = [];
    private _optionOrders: BrokerageOptionOrder[] = [];
    
    optionOrders: BrokerageOptionOrder[];

    @Input()
    set account(value: BrokerageAccount) {
        if (!value) {
            this._stockOrders = [];
            this._optionOrders = [];
            this.groupAndRenderOrders();
            return;
        }
        this._stockOrders = value.stockOrders
        this._optionOrders = value.optionOrders
        console.log("Option orders: " + this.optionOrders)
        this.groupAndRenderOrders()
    }

    private _filteredTickers: string[] = [];

    get filteredTickers(): string[] {
        return this._filteredTickers
    }

    @Input()
    set filteredTickers(val: string[]) {
        this._filteredTickers = val
        this.groupAndRenderOrders()
    }
    @Output()
    orderRecorded = new EventEmitter<BrokerageStockOrder>();

    groupAndRenderOrders() {
        let isTickerVisible = (ticker: string) => this.filteredTickers.length === 0 || this.filteredTickers.indexOf(ticker) !== -1

        if (this._stockOrders) {
            const buys = this._stockOrders.filter(o => o.isBuyOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            const sells = this._stockOrders.filter(o => o.isSellOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            const filled = this._stockOrders.filter(o => !o.isActive && !o.isCancelledOrRejected && isTickerVisible(o.ticker)).sort(orderBy);
            const cancelled = this._stockOrders.filter(o => o.isCancelledOrRejected && isTickerVisible(o.ticker)).sort(orderBy);
            this.groupedOrders = [buys, sells, filled, cancelled]
            this.isEmpty = this.groupedOrders.every(o => o.length == 0)
        }
        
        if (this._optionOrders) {
            this.optionOrders = this._optionOrders.filter(o => isTickerVisible(o.contracts[0].underlyingTicker))
        }
    }

    cancelOrder(orderId: string) {
        this.brokerage.cancelOrder(orderId)
            .subscribe(
                () => this.refreshOrders(),
                err => this.errors = GetErrors(err)
            )
    }
    
    refreshOrders() {
        this.brokerage.brokerageAccount().subscribe(
            a => { this.account = a },
            err => this.errors = GetErrors(err)
        )
    }

    recordOrder(order: BrokerageStockOrder) {
        const obj: stocktransactioncommand = {
            positionId: this.positionId,
            numberOfShares: order.quantity,
            price: order.price,
            date: order.executionTime,
            stopPrice: null,
            brokerageOrderId: order.orderId
        };

        if (order.isBuyOrder) {
            this.stocks.purchase(obj).subscribe(
                _ => this.orderRecorded.emit(),
                err => this.errors = GetErrors(err)
            )
        } else if (order.isSellOrder) {
            this.stocks.sell(obj).subscribe(
                _ => this.orderRecorded.emit(),
                err => this.errors = GetErrors(err)
            )
        }
    }

    getTotal(orders: BrokerageStockOrder[]) {
        return orders
            .filter(o => o.status !== 'CANCELED')
            .reduce((total, order) => total + order.price * order.quantity, 0)
    }
}
