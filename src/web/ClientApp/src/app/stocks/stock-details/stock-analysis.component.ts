import { Component, Input } from '@angular/core';
import { OutcomesReport, Prices, PriceWithDate, StockGaps, StockPercentChangeResponse, StocksService, TickerOutcomes } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css']
})
export class StockAnalysisComponent {
  multipleBarOutcomes: TickerOutcomes;
  
  dailyOutcomesReport : OutcomesReport;
  dailyOutcomes: TickerOutcomes;

  gaps: StockGaps;
  percentChangeDistribution: StockPercentChangeResponse;
  prices: Prices;
  private _ticker: string;
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
        this.getOutcomesReportAllBars();
      }
    );
  }

  private getOutcomesReportAllBars() {
    this.stockService.reportOutcomesAllBars([this.ticker]).subscribe(report => {
      this.gaps = report.gaps[0];
      this.upGaps = this.gaps.gaps.filter(g => g.type === 'Up').map(g => {
        return {
          when: g.bar.date,
          price: g.bar.open
        }
      })
      this.upGapsOpens = this.upGaps.map(g => g.price);
      this.downGaps = this.gaps.gaps.filter(g => g.type === 'Down').map(g => {
        return {
          when: g.bar.date,
          price: g.bar.open
        }
      })
      this.downGapsOpens = this.downGaps.map(g => g.price);
      this.multipleBarOutcomes = report.outcomes[0]
      
      this.getOutcomesReportSingleBarDaily();
    });
  }

  private getOutcomesReportSingleBarDaily() {

    this.stockService.reportOutcomesSingleBarDaily([this.ticker]).subscribe(report => {
      this.dailyOutcomes = report.outcomes[0];
      this.dailyOutcomesReport = report;
      this.percentDistribution();
    });
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
