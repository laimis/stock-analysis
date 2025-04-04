import {Component, EventEmitter, Input, Output} from '@angular/core';
import {
    BrokerageAccount,
    StockViolation
} from 'src/app/services/stocks.service';
import {showElement, toggleVisuallyHidden} from "../../services/utils";

@Component({
    selector: 'app-stock-violations',
    templateUrl: './stock-violations.component.html',
    styleUrls: ['./stock-violations.component.css'],
    standalone: false
})
export class StockViolationsComponent {

    tickersInViolations: string[] = []
    
    @Input()
    brokerageAccount: BrokerageAccount
    
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
        return this.brokerageAccount.stockOrders.some(o => o.ticker == ticker)
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
