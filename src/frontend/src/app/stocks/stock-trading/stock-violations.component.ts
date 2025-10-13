import {Component, EventEmitter, Input, Output} from '@angular/core';
import {
    BrokerageAccount,
    StockViolation
} from "../../services/stocks.service";
import {showElement, toggleVisuallyHidden} from "../../services/utils";
import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { StockLinkAndTradingviewLinkComponent } from "src/app/shared/stocks/stock-link-and-tradingview-link.component";
import { StockTradingNewPositionComponent } from "./stock-trading-new-position.component";
import { BrokerageOrdersComponent } from "src/app/brokerage/brokerage-orders.component";
import { BrokerageNewOrderComponent } from "src/app/brokerage/brokerage-new-order.component";

@Component({
    selector: 'app-stock-violations',
    templateUrl: './stock-violations.component.html',
    styleUrls: ['./stock-violations.component.css'],
    imports: [CurrencyPipe, PercentPipe, StockLinkAndTradingviewLinkComponent, StockTradingNewPositionComponent, BrokerageOrdersComponent, BrokerageNewOrderComponent]
})
export class StockViolationsComponent {

    tickersInViolations: string[] = []
    
    @Input()
    brokerageAccount: BrokerageAccount | null = null 
    
    @Output()
    refreshRequested = new EventEmitter<string>()
    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;

    constructor() {
    }

    private _violations: StockViolation[] = []

    get violations() {
        return this._violations
    }

    @Input()
    set violations(value: StockViolation[]) {
        this._violations = value
        this.tickersInViolations = value.map(v => v.ticker)
    }

    tickerHasOrders(ticker: string) {
        return this.brokerageAccount?.stockOrders.some(o => o.ticker == ticker) ?? false
    }

    getDiffPrice(v: StockViolation) {
        if (v.numberOfShares > 0) {
            return (v.currentPrice - v.pricePerShare) / v.pricePerShare
        } else {
            return (v.pricePerShare - v.currentPrice) / v.currentPrice
        }
    }

    protected readonly showElement = showElement;
}
