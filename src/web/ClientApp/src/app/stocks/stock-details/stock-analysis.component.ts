import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  OutcomesReport,
  OutcomeValueTypeEnum,
  PositionChartInformation,
  StockAnalysisOutcome,
  StockGaps,
  StockPercentChangeResponse,
  StocksService,
  TickerOutcomes
} from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-analysis',
  templateUrl: './stock-analysis.component.html',
  styleUrls: ['./stock-analysis.component.css'],
  providers: [ PercentPipe, CurrencyPipe, DecimalPipe ]
})
export class StockAnalysisComponent {
  multipleBarOutcomes: TickerOutcomes;

  dailyOutcomesReport : OutcomesReport;
  dailyOutcomes: TickerOutcomes;

  gaps: StockGaps;
  percentChangeDistribution: StockPercentChangeResponse;
  private _ticker: string;
  chartInfo: PositionChartInformation

  constructor(
    private stockService : StocksService,
    private percentPipe: PercentPipe,
    private currencyPipe: CurrencyPipe,
    private decimalPipe: DecimalPipe
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
        this.chartInfo = {
          ticker: this.ticker,
          prices: data,
          buyDates: [],
          sellDates: [],
          averageBuyPrice: null,
          stopPrice: null
        }
        this.getOutcomesReportAllBars();
      }
    );
  }

  getValue(o:StockAnalysisOutcome) {
    if (o.valueType === OutcomeValueTypeEnum.Percentage) {
      return this.percentPipe.transform(o.value)
    } else if (o.valueType === OutcomeValueTypeEnum.Currency) {
      return this.currencyPipe.transform(o.value)
    } else {
      return this.decimalPipe.transform(o.value)
    }
  }

  private getOutcomesReportAllBars() {
    this.stockService.reportOutcomesAllBars([this.ticker]).subscribe(report => {
      this.gaps = report.gaps[0];
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
    return outcomes.outcomes.filter(r => r.outcomeType === 'Positive').length;
  }

  negativeCount(outcomes: TickerOutcomes) {
    return outcomes.outcomes.filter(r => r.outcomeType === 'Negative').length;
  }

}
