import { Component, Input } from '@angular/core';
import { PositionInstance, TradingStrategyPerformance } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-trading-strategies',
  templateUrl: './stock-trading-strategies.component.html',
  styleUrls: ['./stock-trading-strategies.component.css']
})
export class StockTradingStrategiesComponent {

  @Input() results : TradingStrategyPerformance[]

  openPositions(positions:PositionInstance[]) {
    return positions.filter(p => !p.isClosed).length;
  }
}