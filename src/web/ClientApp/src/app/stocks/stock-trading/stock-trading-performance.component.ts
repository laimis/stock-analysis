import { Component, OnInit, Input } from '@angular/core';
import { StockTradingPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-performance',
  templateUrl: './stock-trading-performance.component.html',
  styleUrls: ['./stock-trading-performance.component.css']
})
export class StockTradingPerformanceComponent implements OnInit {

	loaded: boolean = false

  ngOnInit() {
  }

  @Input()
  performance: StockTradingPerformance
}
