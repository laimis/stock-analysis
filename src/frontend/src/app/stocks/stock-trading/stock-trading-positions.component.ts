import {Component, EventEmitter, Input, Output} from '@angular/core';
import {toggleVisuallyHidden} from 'src/app/services/utils';
import {BrokerageStockOrder, OutcomeValueTypeEnum, StockPosition, StockQuote} from '../../services/stocks.service';
import {CurrencyPipe, DecimalPipe, PercentPipe} from "@angular/common";


@Component({
    selector: 'app-stock-trading-positions',
    templateUrl: './stock-trading-positions.component.html',
    styleUrls: ['./stock-trading-positions.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe],
    standalone: false
})
export class StockTradingPositionsComponent {

    @Input()
    metricFunc: (p: StockPosition) => any;
    @Input()
    metricType: OutcomeValueTypeEnum;
    @Input()
    positions: StockPosition[]
    @Output()
    positionChanged = new EventEmitter()

    constructor(
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe) {
    }

    private _quotes: Map<string, StockQuote>;

    get quotes() {
        return this._quotes
    }

    @Input()
    set quotes(val: Map<string, StockQuote>) {
        this._quotes = val
    }

    toggleVisibility(elem: HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    getQuote(p: StockPosition) {
        return this.quotes[p.ticker]
    }

    getPrice(p: StockPosition) {
        if (this.quotes) {
            return this.quotes[p.ticker]?.price
        }
        return 0
    }

    getMetricToRender(val: number) {
        if (Number.isFinite(val)) {
            val = Math.round(val * 100) / 100
        }

        if (this.metricType === OutcomeValueTypeEnum.Percentage) {
            return this.percentPipe.transform(val, '1.0-2')
        } else if (this.metricType === OutcomeValueTypeEnum.Currency) {
            return this.currencyPipe.transform(val)
        } else if (this.metricType === OutcomeValueTypeEnum.String) {
            return val
        } else {
            return this.decimalPipe.transform(val)
        }
    }
}

