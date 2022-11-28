import { Component, OnInit } from '@angular/core';
import { StocksService, TradingStrategyPerformance } from '../services/stocks.service';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  results: TradingStrategyPerformance[];
  
  constructor(private stocks:StocksService) { }

  ngOnInit() {
    this.stocks.simulatePositions(20).subscribe( results => {
        this.results = results
      });
  }
}

