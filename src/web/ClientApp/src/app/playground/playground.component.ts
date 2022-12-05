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

    this.stocks.simulatePositions(true, numberOfTrades).subscribe( results => {
        this.results = results
      });
  }

  openPositions(positions:PositionInstance[]) {
    return positions.filter(p => !p.isClosed).length;
  }

  getClassBasedOnProfit(position:PositionInstance) {
    return (position.profit + position.unrealizedProfit) > 0 ? 'table-success' : 'table-danger';
  }
}

