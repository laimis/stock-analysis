import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { OutcomeValueTypeEnum, StockPosition, StockQuote} from '../../services/stocks.service';
import {CurrencyPipe, DecimalPipe, PercentPipe} from "@angular/common";
import {toggleVisuallyHidden} from "../../services/utils";
import { StockTradingPositionComponent } from "./stock-trading-position.component";
import { StockLinkComponent } from "src/app/shared/stocks/stock-link.component";


@Component({
    selector: 'app-stock-trading-positions',
    templateUrl: './stock-trading-positions.component.html',
    styleUrls: ['./stock-trading-positions.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe],
    imports: [StockTradingPositionComponent, StockLinkComponent, CurrencyPipe]
})
export class StockTradingPositionsComponent {
    private percentPipe = inject(PercentPipe);
    private currencyPipe = inject(CurrencyPipe);
    private decimalPipe = inject(DecimalPipe);


    @Input()
    metricFunc: (p: StockPosition) => any | null = () => null;
    @Input()
    metricType: OutcomeValueTypeEnum = OutcomeValueTypeEnum.Number;
    @Input()
    positions: StockPosition[] = [];
    @Output()
    positionChanged = new EventEmitter()

    private _quotes: Map<string, StockQuote> = new Map<string, StockQuote>()

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
        return this.quotes.get(p.ticker)
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

