import {Component, EventEmitter, Input, Output} from '@angular/core';
import {toggleVisuallyHidden} from 'src/app/services/utils';
import {
    BrokerageOrder, OutcomeValueTypeEnum,
    PositionInstance,
    StockQuote
} from '../../services/stocks.service';


@Component({
    selector: 'app-stock-trading-positions',
    templateUrl: './stock-trading-positions.component.html',
    styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionsComponent {
    @Input()
    metricFunc: (p: PositionInstance) => any;

    @Input()
    positions: PositionInstance[]

    @Input()
    orders: BrokerageOrder[];

    private _quotes: Map<string, StockQuote>;
    @Input()
    set quotes(val: Map<string, StockQuote>) {
        this._quotes = val
    }

    get quotes() {
        return this._quotes
    }

    @Output()
    brokerageOrdersChanged = new EventEmitter<string>()

    toggleVisibility(elem: HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    getQuote(p: PositionInstance) {
        return this.quotes[p.ticker]
    }

    getPrice(p: PositionInstance) {
        if (this.quotes) {
            return this.quotes[p.ticker]?.price
        }
        return 0
    }
}

