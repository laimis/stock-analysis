import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DailyScore, StocksService, TradingStrategyPerformance } from '../services/stocks.service';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  results: TradingStrategyPerformance[];
  dailyScores: DailyScore[];
  
  constructor(
    private stocks:StocksService,
    private route:ActivatedRoute) { }

  ticker:string;
  startDate:string;

  ngOnInit() {
    this.ticker = this.route.snapshot.queryParamMap.get('ticker');
    this.startDate = this.route.snapshot.queryParamMap.get('startDate');
    
    this.stocks.reportDailyOutcomesReport(
      this.ticker, this.startDate).subscribe( results => {
        this.dailyScores = results.dailyScores;
      });
  }

  getDailyScores() {
    return this.dailyScores.map(d => d.score)
  }

  getDailyScoresDates() {
    return this.dailyScores.map(d => d.date.split('T')[0])
  }
}

