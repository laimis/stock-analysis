import {Component, Input} from '@angular/core';
import {
    DataPointContainer,
    StockTradingPerformance,
    StockTradingPerformanceCollection
} from 'src/app/services/stocks.service';


@Component({
    selector: 'app-stock-trading-performance',
    templateUrl: './stock-trading-performance.component.html',
    styleUrls: ['./stock-trading-performance.component.css'],
    standalone: false
})
export class StockTradingPerformanceComponent {

    trends: DataPointContainer[]
    performanceSelection: StockTradingPerformance
    performanceTitle = "YTD"

    private _performance: StockTradingPerformanceCollection

    get performance() {
        return this._performance
    }

    @Input()
    set performance(value: StockTradingPerformanceCollection) {
        this._performance = value
        this.selectPerformanceToRenderBasedOnTradePeriodFilter()
    }

    selectPerformanceToRenderBasedOnTradePeriodFilter() {
        let index = this.performance.performances.findIndex(p => p.name == this.performanceTitle)
        this.performanceSelection = this.performance.performances[index]
        this.performanceTitle = this.performanceSelection.name
        this.trends = this.performance.trends[index]
    }

    performanceTradePeriodChanged(value: string) {
        if (value != this.performanceTitle) {
            this.performanceTitle = value
            this.selectPerformanceToRenderBasedOnTradePeriodFilter()
        }
    }
}
