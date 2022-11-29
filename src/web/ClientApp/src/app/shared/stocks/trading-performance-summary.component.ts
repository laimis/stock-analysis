import { Component, Input } from '@angular/core';
import { StockTradingPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-trading-performance-summary',
  templateUrl: './trading-performance-summary.component.html',
})
export class TradingPerformanceSummaryComponent {

  @Input()
  public title: string;

  @Input()
  public performance: StockTradingPerformance
}

