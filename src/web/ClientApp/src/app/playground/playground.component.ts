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

  numberOfTrades:number = 40;
  closePositions:boolean = false;

  ngOnInit() {
    var n = this.route.snapshot.queryParamMap.get('n');
    this.closePositions = this.route.snapshot.queryParamMap.get('closePositions') === "true";
    
    if (n) {
      this.numberOfTrades = parseInt(n);
    }

    this.stocks.simulatePositions(this.closePositions, this.numberOfTrades).subscribe( results => {
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

