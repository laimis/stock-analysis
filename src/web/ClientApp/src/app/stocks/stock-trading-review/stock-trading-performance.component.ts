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
    this.selectTrendsToRenderBasedOnTradePeriodFilter()
  }
  get performance() {
    return this._performance
  }

  tradePeriod = "ytd"
  trends:DataPointContainer[]
  selectTrendsToRenderBasedOnTradePeriodFilter() {
    if (this.tradePeriod == "1y") {
      this.trends = this.performance.trendsOneYear
    } else if (this.tradePeriod == "ytd") {
      this.trends = this.performance.trendsYTD
    } else if (this.tradePeriod == "2m") {
      this.trends = this.performance.trendsTwoMonths
    } else if (this.tradePeriod == "all") {
      this.trends = this.performance.trendsAll
    } else if (this.tradePeriod == "last20") {
      this.trends = this.performance.trendsLast20
    } else if (this.tradePeriod == "last50") {
      this.trends = this.performance.trendsLast50
    } else if (this.tradePeriod == "last100") {
      this.trends = this.performance.trendsLast100
    }
  }

  performanceTradePeriod = "last20"
  performanceSelection:StockTradingPerformance
  performanceTitle = "Last 20 Trades"
  selectPerformanceToRenderBasedOnTradePeriodFilter() {
    if (this.performanceTradePeriod == "last20") {
      this.performanceSelection = this.performance.performanceLast20
      this.performanceTitle = "Last 20 Trades"
    } else if (this.performanceTradePeriod == "last50") {
      this.performanceSelection = this.performance.performanceLast50
      this.performanceTitle = "Last 50 Trades"
    } else if (this.performanceTradePeriod == "last100") {
      this.performanceSelection = this.performance.performanceLast100
      this.performanceTitle = "Last 100 Trades"
    } else if (this.performanceTradePeriod == "all") {
      this.performanceSelection = this.performance.performanceAll
      this.performanceTitle = "All Trades"
    } else if (this.performanceTradePeriod == "ytd") {
      this.performanceSelection = this.performance.performanceYTD
      this.performanceTitle = "Year To Date"
    } else if (this.performanceTradePeriod == "1y") {
      this.performanceSelection = this.performance.performanceOneYear
      this.performanceTitle = "One Year"
    }
  }

  performanceTradePeriodChanged(value:string) {
    if (value != this.performanceTradePeriod) {
      this.performanceTradePeriod = value
      this.selectPerformanceToRenderBasedOnTradePeriodFilter()
    }
  }

  tradePeriodChanged(value:string) {
    if (value != this.tradePeriod) {
      this.tradePeriod = value
      this.selectTrendsToRenderBasedOnTradePeriodFilter()
    }
  }


}
