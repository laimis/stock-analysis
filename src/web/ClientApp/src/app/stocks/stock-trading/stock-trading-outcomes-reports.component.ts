import { Component, Input, OnInit } from '@angular/core';
import { GetErrors } from 'src/app/services/utils';
import { StocksService, OutcomesReport, PositionInstance, StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-outcomes-reports',
  templateUrl: './stock-trading-outcomes-reports.component.html',
  styleUrls: ['./stock-trading-outcomes-reports.component.css']
})
export class StockPositionReportsComponent {


  sortColumn: string
  sortDirection: number = -1
  allBarsReport: OutcomesReport;
  singleBarReportDaily: OutcomesReport;
  singleBarReportWeekly: OutcomesReport;
  positionsReport: OutcomesReport;
  gaps: StockGaps[] = [];
  tickerFilter: string;


	constructor(private service : StocksService){}

  @Input()
  set dailyAnalysis(value:PositionInstance[]) {
    if (value) {
      this.loadPositionData(value)
    }
  }

  @Input()
  set allTimeAnalysis(value:PositionInstance[]) {
    if (value) {
      this.loadAllTimeData(value)
    }
  }

  tickers: string[] = []
  errors: string[] = null
  loadDailyData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarDaily(this.tickers).subscribe(
      report => {
        this.singleBarReportDaily = report
        this.loadWeeklyData(positions)
      },
      error => {
        this.handleApiError("Unable to load daily data", error)
        this.loadWeeklyData(positions)
      }
    )
  }

  loadWeeklyData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarWeekly(this.tickers).subscribe(report => {
      this.singleBarReportWeekly = report
    }, error => {
      this.handleApiError("Unable to load weekly data", error)
    })

  }

  loadPositionData(positions:PositionInstance[]) {
    this.service.reportPositions().subscribe(report => {
      this.positionsReport = report
      this.loadDailyData(positions)
    }, error => {
      this.handleApiError("Unable to load position reports", error)
      this.loadDailyData(positions)
    })
  }

  private handleApiError(errorMessage: string, error: any) {
    var forConsole = GetErrors(error)
    forConsole.forEach(e => console.log(e))
    this.errors = [errorMessage]
  }

  loadAllTimeData(positions:PositionInstance[]) {
    this.tickers = positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(this.tickers).subscribe(report => {
      this.allBarsReport = report
      this.gaps = report.gaps
    }, error => {
      this.handleApiError("Unable to load all bars", error)
    })
  }

  onTickerChange(ticker: string) {
    this.tickerFilter = ticker
  }
}
