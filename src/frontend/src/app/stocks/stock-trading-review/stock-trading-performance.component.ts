import {Component, Input} from '@angular/core';
import {
    DataPointContainer,
    StockTradingPerformance,
    StockTradingPerformanceCollection
} from "../../services/stocks.service";
import { LineChartComponent } from "src/app/shared/line-chart/line-chart.component";
import { TradingPerformanceSummaryComponent } from "src/app/shared/stocks/trading-performance-summary.component";


@Component({
    selector: 'app-stock-trading-performance',
    templateUrl: './stock-trading-performance.component.html',
    styleUrls: ['./stock-trading-performance.component.css'],
    standalone: true,
    imports: [LineChartComponent, TradingPerformanceSummaryComponent]
})
export class StockTradingPerformanceComponent {

    trends: DataPointContainer[] = []
    performanceSelection: StockTradingPerformance | null = null
    performanceTitle = "YTD" // default selection

    private _performance: StockTradingPerformanceCollection | null = null

    get performance() {
        return this._performance!
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
