import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DailyScore, Prices, StocksService } from '../services/stocks.service';


@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  dailyScores: DailyScore[];
  tickers: string[];
  prices: Prices;
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  ticker:string;
  startDate:string;

  ngOnInit() {
    var tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    if (tickerParam) {
      this.tickers = tickerParam.split(',');
    }
    this.stocks.getStockPrices(this.tickers[0], 365).subscribe(result => {
      this.prices = result
    });
    
  }
}

