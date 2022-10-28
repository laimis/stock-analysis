import { Component, Input, OnInit } from '@angular/core';
import { OutcomesAnalysisReport, StocksService, TickerOutcomes } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent implements OnInit {
  multipleBarOutcomes: TickerOutcomes;
  dailyOutcomes : TickerOutcomes;
  dailyAnalysis: OutcomesAnalysisReport;
  weeklyAnalysis: OutcomesAnalysisReport;

  constructor(
    private stockService : StocksService
  ) { }

  @Input()
  ticker: string;

  ngOnInit(): void {
    this.allTimeOutcomes();
  }

  private allTimeOutcomes() {
    this.stockService.reportTickerOutcomesAllTime(this.ticker).subscribe(
      data => {
        this.multipleBarOutcomes = data[0];
        this.dayOutcomes();
      }
    );
  }

  private dayOutcomes() {
    this.stockService.reportTickerOutcomesDay(this.ticker).subscribe(
      data => {
        this.dailyOutcomes = data[0];
        this.dailyAnalysisReport();
      }
    );
  }

  private dailyAnalysisReport() {
    this.stockService.reportTickerAnalysisDaily(this.ticker).subscribe(
      data => {
        this.dailyAnalysis = data;
        this.weeklyAnalysisReport();
      }
    );
  }

  private weeklyAnalysisReport() {
    this.stockService.reportTickerAnalysisWeekly(this.ticker).subscribe(
      data => {
        this.weeklyAnalysis = data;
      }
    );
  }

  positiveCount(outcomes: TickerOutcomes) {
    return outcomes.outcomes.filter(r => r.type === 'Positive').length;
  }

  negativeCount(outcomes: TickerOutcomes) {
    return outcomes.outcomes.filter(r => r.type === 'Negative').length;
  }

}
