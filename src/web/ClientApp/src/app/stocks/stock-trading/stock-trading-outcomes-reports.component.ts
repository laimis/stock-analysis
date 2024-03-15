import { Component, Input, OnInit } from '@angular/core';
import { GetErrors } from 'src/app/services/utils';
import { StocksService, OutcomesReport, PositionInstance, StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-outcomes-reports',
  templateUrl: './stock-trading-outcomes-reports.component.html',
  styleUrls: ['./stock-trading-outcomes-reports.component.css']
})
export class StockPositionReportsComponent {

  allBarsReport: OutcomesReport;
  singleBarReportDaily: OutcomesReport;
  singleBarReportWeekly: OutcomesReport;
  positionsReport: OutcomesReport;
  gaps: StockGaps[] = [];
  tickerFilter: string;

  loading = {
    daily: false,
    weekly: false,
    allBars: false,
    positions: false
  }

  errors = {
    daily: null,
    weekly: null,
    allBars: null,
    positions: null
  }


	constructor(private service : StocksService){}

  @Input()
  set dailyAnalysis(value:PositionInstance[]) {
    if (value) {
      this.loading.daily = true
      this.loading.weekly = true
      this.loading.positions = true
      this.loadPositionData(value)
    }
  }

  @Input()
  set allTimeAnalysis(value:PositionInstance[]) {
    if (value) {
      this.loading.allBars = true
      this.loadAllTimeData(value)
    }
  }

  tickers: string[] = []
  loadDailyData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarDaily(this.tickers).subscribe(
      report => {
        this.loading.daily = false
        this.singleBarReportDaily = report
        this.loadWeeklyData(positions)
      },
      error => {
        this.loading.daily = false
        this.handleApiError("Unable to load daily data", error, (e) => this.errors.daily = e)
        this.loadWeeklyData(positions)
      }
    )
  }

  loadWeeklyData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarWeekly(this.tickers).subscribe(report => {
      this.loading.weekly = false
      this.singleBarReportWeekly = report
    }, error => {
      this.loading.weekly = false
      this.handleApiError("Unable to load weekly data", error, (e) => this.errors.weekly = e)
    })

  }

  loadPositionData(positions:PositionInstance[]) {
    this.service.reportPositions().subscribe(report => {
      this.loading.positions = false
      this.positionsReport = report
      this.loadDailyData(positions)
    }, error => {
      this.loading.positions = false
      this.handleApiError("Unable to load position reports", error, (e) => this.errors.positions = e)
      this.loadDailyData(positions)
    })
  }

  private handleApiError(errorMessage: string, error: any, assignFunc : (error:any) => void) {
    const extractedErrors = GetErrors(error);
    extractedErrors.forEach(e => console.log(e))
    const fullError = errorMessage + ": " + extractedErrors.join(", ")
    assignFunc([fullError])
  }

  loadAllTimeData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(this.tickers).subscribe(report => {
      this.loading.allBars = false
      this.allBarsReport = report
      this.gaps = report.gaps
    }, error => {
      this.loading.allBars = false
      this.handleApiError("Unable to load all bars", error, (e) => this.errors.allBars = e)
    })
  }

  onTickerChange(ticker: string) {
    this.tickerFilter = ticker
  }
}
