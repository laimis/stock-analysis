import { DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Evaluations, Prices, PriceWithDate, StockGaps, StockPercentChangeResponse, StocksService, TickerOutcomes } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent {
  multipleBarOutcomes: TickerOutcomes;
  dailyOutcomes : TickerOutcomes;
  dailyAnalysis: Evaluations;
  weeklyAnalysis: Evaluations;
  gaps: StockGaps;
  percentChangeDistribution: StockPercentChangeResponse;
  prices: Prices;
  private _ticker: string;
  gapOpens: number[] = [];
  upGaps: PriceWithDate[] = [];
  downGaps: PriceWithDate[] = [];
  upGapsOpens: number[] = [];
  downGapsOpens: number[] = [];

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
    
  }

  private dayOutcomes() {
    
  }

  private dailyAnalysisReport() {
    
  }

  private weeklyAnalysisReport() {
    
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
