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
    var number = 20;
    if (n) {
      number = parseInt(n);
    }

    this.stocks.simulatePositions(number).subscribe( results => {
        this.results = results
      });
  }

  openPositions(positions:PositionInstance[]) {
    return positions.filter(p => !p.isClosed).length;
  }
}

