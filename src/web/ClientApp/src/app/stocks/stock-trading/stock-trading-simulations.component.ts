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
  closePositions:boolean = true;

  ngOnInit() {
    var n = this.route.snapshot.queryParamMap.get('n');
    var closePositionsParam = this.route.snapshot.queryParamMap.get('closePositions');

    if (closePositionsParam) {
      this.closePositions = closePositionsParam == 'true';
    }
    
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

    var simulatedProfit = simulatedPosition.combinedProfit;
    var actualProfit = actualPosition.combinedProfit;

    return actualProfit >= simulatedProfit ? 'bg-success' : '';
  }

  getExportUrl() {
    return this.stocks.simulatePositionsExportUrl(this.closePositions, this.numberOfTrades);
  }
}

