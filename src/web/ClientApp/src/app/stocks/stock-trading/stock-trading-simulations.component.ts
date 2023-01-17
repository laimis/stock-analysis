import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PositionInstance, StocksService, TradingStrategyPerformance } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-simulations',
  templateUrl: './stock-trading-simulations.component.html',
  styleUrls: ['./stock-trading-simulations.component.css']
})

export class StockTradingSimulationsComponent implements OnInit {
  results: TradingStrategyPerformance[];
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  numberOfTrades:number = 40;
  closePositions:boolean = false;

  ngOnInit() {
    var n = this.route.snapshot.queryParamMap.get('n');
    this.closePositions = this.route.snapshot.queryParamMap.get('closePositions') === "true";
    
    if (n) {
      this.numberOfTrades = parseInt(n);
    }

    this.stocks.simulatePositions(this.closePositions, this.numberOfTrades).subscribe( results => {
        this.results = results.sort((a,b) => b.performance.profit - a.performance.profit);
      });
  }

  openPositions(positions:PositionInstance[]) {
    return positions.filter(p => !p.isClosed).length;
  }

  backgroundCssClassForActual(results:TradingStrategyPerformance[], strategyIndex: number, positionIndex: number) {
    var simulatedPosition = results[strategyIndex].positions[positionIndex];
    var actualPosition = results[0].positions[positionIndex];

    var simulatedProfit = simulatedPosition.profit + simulatedPosition.unrealizedProfit;
    var actualProfit = actualPosition.profit + actualPosition.unrealizedProfit;

    return actualProfit >= simulatedProfit ? 'bg-success' : '';
  }
}
