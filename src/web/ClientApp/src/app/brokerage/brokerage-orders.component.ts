import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOrder, stocktransactioncommand} from 'src/app/services/stocks.service';
import {GetErrors} from '../services/utils';
import {BrokerageService} from "../services/brokerage.service";
import {StockPositionsService} from "../services/stockpositions.service";
import {CurrencyPipe, DatePipe, NgClass, NgIf} from "@angular/common";
import {StockLinkAndTradingviewLinkComponent} from "../shared/stocks/stock-link-and-tradingview-link.component";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";

let orderBy = (a: BrokerageOrder, b: BrokerageOrder) => a.ticker.localeCompare(b.ticker)

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
    groupedOrders: BrokerageOrder[][];
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

    private _orders: BrokerageOrder[] = [];

    @Input()
    set orders(value: BrokerageOrder[]) {
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
            const filledBuys = this._orders.filter(o => o.isBuyOrder && !o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            const filledSells = this._orders.filter(o => o.isSellOrder && !o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
            this.groupedOrders = [buys, sells, filledBuys, filledSells]
            this.isEmpty = this.groupedOrders.every(o => o.length == 0)
        }
    }

    cancelOrder(orderId: string) {
        this.brokerage.cancelOrder(orderId)
            .subscribe(
                () => {
                    this.brokerage.brokerageAccount().subscribe(
                        a => {
                            this.orders = a.orders
                        },
                        err => this.errors = GetErrors(err)
                    )
                    this.ordersChanged.emit()
                },
                err => this.errors = GetErrors(err)
            )
    }

    recordOrder(order: BrokerageOrder) {
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

    getTotal(orders: BrokerageOrder[]) {
        return orders
            .filter(o => o.status !== 'CANCELED')
            .reduce((total, order) => total + order.price * order.quantity, 0)
    }
}
