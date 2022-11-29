import { Component, OnInit, Input } from '@angular/core';
import { PositionInstance, StocksService, TradingStrategyPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent implements OnInit {
  performances: TradingStrategyPerformance[];

  // constructor with services
  constructor(
    private stocksService: StocksService
  ) { }

	ngOnInit() {
    this.stocksService.simulatePositions(20).subscribe(performance => {
      this.performances = performance;
      });
  }

  @Input()
  positions: PositionInstance[]
}
