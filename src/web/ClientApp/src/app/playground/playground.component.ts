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

    // if start date is not set, set it to 30 days ago
    if (!this.startDate) {
      var d = new Date();
      d.setDate(d.getDate() - 30);
      this.startDate = d.toISOString().split('T')[0];
    }
    
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

