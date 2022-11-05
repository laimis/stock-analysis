import { DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { OutcomesAnalysisReport, OutcomesReport, Prices, PriceWithDate, StockGaps, StockPercentChangeResponse, StocksService, TickerOutcomes } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent {
  multipleBarOutcomes: TickerOutcomes;
  dailyOutcomes : TickerOutcomes;
  dailyAnalysis: OutcomesAnalysisReport;
  weeklyAnalysis: OutcomesAnalysisReport;
  gaps: StockGaps;
  percentChangeDistribution: StockPercentChangeResponse;
  prices: Prices;
  private _ticker: string;
  gapOpens: number[] = [];
  upGaps: PriceWithDate[] = [];
  downGaps: PriceWithDate[] = [];

  constructor(
    private stockService : StocksService
  ) { }

  @Input()
  set ticker(value:string) {
    this._ticker = value;
    this.getPrices();
  }
  get ticker() {
    return this._ticker;
  }

  private getPrices() {
    this.stockService.getStockPrices(this.ticker, 365).subscribe(
      data => {
        this.prices = data;
        this.allTimeOutcomes();
      }
    );
  }

  private allTimeOutcomes() {
    this.stockService.reportTickerOutcomesAllTime(this.ticker, true).subscribe(
      (data:OutcomesReport) => {
        this.multipleBarOutcomes = data.outcomes[0];
        this.gaps = data.gaps[0];
        this.dayOutcomes();
        this.gapOpens = this.gaps.gaps.map(g => g.bar.open);
        this.upGaps = this.gaps.gaps.filter(g => g.type === 'Up').map(g => {
          return {
            when: g.bar.date,
            price: g.bar.open
          }
        })
        this.downGaps = this.gaps.gaps.filter(g => g.type === 'Down').map(g => {
          return {
            when: g.bar.date,
            price: g.bar.open
          }
        })
      }
    );
  }

  private dayOutcomes() {
    this.stockService.reportTickerOutcomesDay(this.ticker, false).subscribe(
      (data:OutcomesReport) => {
        this.dailyOutcomes = data.outcomes[0];
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
        this.percentDistribution();
      }
    );
  }

  private percentDistribution() {
    this.stockService.reportTickerPercentChangeDistribution(this.ticker).subscribe(
      data => {
        this.percentChangeDistribution = data;
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
