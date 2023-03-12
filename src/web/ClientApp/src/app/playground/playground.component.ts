import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DailyScore, SECFilings, StocksService, TradingStrategyPerformance } from '../services/stocks.service';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  results: SECFilings[] = [];
  dailyScores: DailyScore[];
  tickers: string[];
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  ticker:string;
  startDate:string;

  ngOnInit() {
    var tickerParam = this.route.snapshot.queryParamMap.get('tickers');
    if (tickerParam) {
      this.tickers = tickerParam.split(',');

      this.fetchSecFilings(this.tickers)
    }
  }

  fetchSecFilings(tickers:string[]) {
    if (tickers.length == 0) {
      return;
    }

    // get the filing for the first ticker
    this.stocks.getStockSECFilings(tickers[0]).subscribe(
      filings => {
        console.log("Adding filings for " + filings.ticker)
        this.results.push(filings);
        this.fetchSecFilings(tickers.slice(1));
      },
      error => {
        console.log("Error fetching filings for " + tickers[0])
        this.fetchSecFilings(tickers.slice(1));
      }
    );
  }

  getDailyScoresDates() {
    return this.dailyScores.map(d => d.date.split('T')[0])
  }
}

