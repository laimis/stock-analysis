import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PositionInstance, StocksService, TradingStrategyPerformance } from '../services/stocks.service';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  results: TradingStrategyPerformance[];
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  ngOnInit() {
    var n = this.route.snapshot.queryParamMap.get('n');
    var numberOfTrades = 20;
    if (n) {
      numberOfTrades = parseInt(n);
    }

    this.stocks.simulatePositions(false, numberOfTrades).subscribe( results => {
        this.results = results
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

