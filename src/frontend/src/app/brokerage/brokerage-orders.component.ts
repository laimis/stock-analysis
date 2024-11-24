import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageStockOrder, stocktransactioncommand} from 'src/app/services/stocks.service';
import {GetErrors} from '../services/utils';
import {BrokerageService} from "../services/brokerage.service";
import {StockPositionsService} from "../services/stockpositions.service";
import {CurrencyPipe, DatePipe, NgClass, NgIf} from "@angular/common";
import {StockLinkAndTradingviewLinkComponent} from "../shared/stocks/stock-link-and-tradingview-link.component";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";

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
    styleUrls: ['./brokerage-orders.component.css'],
    imports: [
        NgClass,
        StockLinkAndTradingviewLinkComponent,
        CurrencyPipe,
        DatePipe,
        ErrorDisplayComponent,
        NgIf
    ],
    standalone: true
})
export class BrokerageOrdersComponent {
    groupedOrders: BrokerageStockOrder[][];
    isEmpty: boolean = false;
    errors: string[];
    @Input()
    justOrders: boolean = false;
    @Input()
    positionId: string
    @Output()
    ordersChanged = new EventEmitter()

    constructor(private brokerage: BrokerageService, private stocks: StockPositionsService) {
    }

    private _orders: BrokerageStockOrder[] = [];

    @Input()
    set orders(value: BrokerageStockOrder[]) {
        this._orders = value
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

    groupAndRenderOrders() {
        let isTickerVisible = (ticker: string) => this.filteredTickers.length === 0 || this.filteredTickers.indexOf(ticker) !== -1

        if (this._orders) {
            const buys = this._orders.filter(o => o.isBuyOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            const sells = this._orders.filter(o => o.isSellOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            const filled = this._orders.filter(o => !o.isActive && !o.isCancelledOrRejected && isTickerVisible(o.ticker)).sort(orderBy);
            const cancelled = this._orders.filter(o => o.isCancelledOrRejected && isTickerVisible(o.ticker)).sort(orderBy);
            this.groupedOrders = [buys, sells, filled, cancelled]
            this.isEmpty = this.groupedOrders.every(o => o.length == 0)
        }
    }

    cancelOrder(orderId: string) {
        this.brokerage.cancelOrder(orderId)
            .subscribe(
                () => {
                    this.brokerage.brokerageAccount().subscribe(
                        a => { this.orders = a.stockOrders },
                        err => this.errors = GetErrors(err)
                    )
                    this.ordersChanged.emit()
                },
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
                _ => this.ordersChanged.emit(),
                err => this.errors = GetErrors(err)
            )
        } else if (order.isSellOrder) {
            this.stocks.sell(obj).subscribe(
                _ => this.ordersChanged.emit(),
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
