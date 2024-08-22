import {Component, Input} from '@angular/core';
import {TradingStrategyPerformance} from 'src/app/services/stocks.service';

@Component({
    selector: 'app-stock-trading-strategies',
    templateUrl: './stock-trading-strategies.component.html',
    styleUrls: ['./stock-trading-strategies.component.css']
})
export class StockTradingStrategiesComponent {

    sortedResults: TradingStrategyPerformance[]
    private _sortDirection = 1
    private _sortProperty = "strategyName"

    private _results: TradingStrategyPerformance[]

    @Input()
    set results(value: TradingStrategyPerformance[]) {
        this._results = value
        this.sortedResults = value
    }

    sort(propertyName) {
        if (this._sortProperty == propertyName) {
            this._sortDirection *= -1
        } else {
            this._sortDirection = 1
        }

        this._sortProperty = propertyName

        this.doSort()
    }

    doSort() {
        this.sortedResults = this._results.sort((a, b) => {
            let aVal = a[this._sortProperty]
            if (!aVal) {
                aVal = a.performance[this._sortProperty]
            }
            let bVal = b[this._sortProperty]
            if (!bVal) {
                bVal = b.performance[this._sortProperty]
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
