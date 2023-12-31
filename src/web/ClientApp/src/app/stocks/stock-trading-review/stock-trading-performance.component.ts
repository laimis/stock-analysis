import { Component, Input } from '@angular/core';
import {
  DataPointContainer,
  StockTradingPerformance,
  StockTradingPerformanceCollection
} from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-performance',
  templateUrl: './stock-trading-performance.component.html',
  styleUrls: ['./stock-trading-performance.component.css']
})
export class StockTradingPerformanceComponent {

  private _performance:StockTradingPerformanceCollection

  @Input()
  set performance(value:StockTradingPerformanceCollection) {
    this._performance = value
    this.selectPerformanceToRenderBasedOnTradePeriodFilter()
  }
  get performance() {
    return this._performance
  }

  trends:DataPointContainer[]

  performanceTradePeriod = "Last 20"
  performanceSelection:StockTradingPerformance
  performanceTitle = "Last 20"
  selectPerformanceToRenderBasedOnTradePeriodFilter() {
    let index = this.performance.performances.findIndex(p => p.name == this.performanceTradePeriod)
    this.performanceSelection = this.performance.performances[index]
    this.performanceTitle = this.performanceSelection.name
    this.trends = this.performance.trends[index]
  }

  performanceTradePeriodChanged(value:string) {
    if (value != this.performanceTradePeriod) {
      this.performanceTradePeriod = value
      this.selectPerformanceToRenderBasedOnTradePeriodFilter()
    }
  }
}
