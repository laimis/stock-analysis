import { Component, Input } from '@angular/core';
import { PositionInstance, TradingStrategyPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent {
  performances: TradingStrategyPerformance[];

  @Input()
  positions: PositionInstance[]
}
