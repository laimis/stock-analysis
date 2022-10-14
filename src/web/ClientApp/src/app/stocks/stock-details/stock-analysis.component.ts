import { Component, Input, OnInit } from '@angular/core';
import { StockAnalysis, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent implements OnInit {
  analysis: StockAnalysis;
  daily_analysis : StockAnalysis;

  constructor(
    private stockService : StocksService
  ) { }

  @Input()
  ticker: string;

  ngOnInit(): void {
    this.stockService.getStockAnalysis(this.ticker).subscribe(
      data => {
        this.analysis = data
      }
    );

    this.stockService.getStockDailyAnalysis(this.ticker).subscribe(
      data => {
        this.daily_analysis = data
      }
    );
  }

  positiveCount(analysis: StockAnalysis) {
    return analysis.outcomes.filter(r => r.type === 'Positive').length;
  }

  negativeCount(analysis: StockAnalysis) {
    return analysis.outcomes.filter(r => r.type === 'Negative').length;
  }

}
