import {Component, Input, ChangeDetectionStrategy} from '@angular/core';
import {TradingStrategyPerformance} from '../../services/stocks.service';
import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';

@Component({
    selector: 'app-stock-trading-strategies',
    templateUrl: './stock-trading-strategies.component.html',
    styleUrls: ['./stock-trading-strategies.component.css'],
    imports: [DecimalPipe, CurrencyPipe, PercentPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true
})
export class StockTradingStrategiesComponent {

    sortedResults: TradingStrategyPerformance[] = []
    private _sortDirection = 1
    private _sortProperty = "strategyName"

    private _results: TradingStrategyPerformance[] = []

    @Input()
    set results(value: TradingStrategyPerformance[]) {
        this._results = value
        this.sortedResults = value
    }

    sort(propertyName:string) {
        if (this._sortProperty == propertyName) {
            this._sortDirection *= -1
        } else {
            this._sortDirection = 1
        }

        this._sortProperty = propertyName

        this.doSort()
    }

    doSort() {
        this.sortedResults = this._results.sort((a: TradingStrategyPerformance, b: TradingStrategyPerformance) => {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            let aVal = (a as unknown as Record<string, unknown>)[this._sortProperty]
            if (!aVal) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                aVal = (a as unknown as Record<string, Record<string, unknown>>).performance[this._sortProperty]
            }
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            let bVal = (b as unknown as Record<string, unknown>)[this._sortProperty]
            if (!bVal) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                bVal = (b as unknown as Record<string, Record<string, unknown>>).performance[this._sortProperty]
            }

            if (aVal < bVal) {
                return -1 * this._sortDirection
            }
            if (aVal > bVal) {
                return 1 * this._sortDirection
            }
            return 0
        })
    }

    protected readonly Number = Number;
}
