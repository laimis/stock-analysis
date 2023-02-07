import { Component, Input, OnInit } from '@angular/core';
import { GetErrors } from 'src/app/services/utils';
import { StocksService, OutcomesReport, PositionInstance, StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-outcomes-reports',
  templateUrl: './stock-trading-outcomes-reports.component.html',
  styleUrls: ['./stock-trading-outcomes-reports.component.css']
})
export class StockPositionReportsComponent implements OnInit {

  
  sortColumn: string
  sortDirection: number = -1
  allBarsReport: OutcomesReport;
  singleBarReportDaily: OutcomesReport;
  singleBarReportWeekly: OutcomesReport;
  positionsReport: OutcomesReport;
  gaps: StockGaps[] = [];
  

	constructor(private service : StocksService){}

  @Input()
  dailyMode: boolean = false

  @Input()
  allTimeMode: boolean = false

  @Input()
  positions: PositionInstance[] = []

  errors: string[] = []


	ngOnInit(): void {
    
    if (this.allTimeMode) {
      this.loadAllTimeData()
    }

    if (this.dailyMode) {
      this.loadPositionData()
    }
  }

  loadDailyData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarDaily(tickers).subscribe(
      report => {
        this.singleBarReportDaily = report
      },
      error => {
        this.handleApiError("Unable to load daily data", error)
      }
    )
  }

  loadWeeklyData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarWeekly(tickers).subscribe(report => {
      this.singleBarReportWeekly = report
    }, error => {
      this.handleApiError("Unable to load weekly data", error)
    })

  }

  loadPositionData() {
    this.service.reportPositions().subscribe(report => {
      this.positionsReport = report
    }, error => {
      this.handleApiError("Unable to load position reports", error)
    },
    () => {
      console.log("calling load daily data")
      this.loadDailyData()
    })
  }

  private handleApiError(errorMessage: string, error: any) {
    var forConsole = GetErrors(error)
    forConsole.forEach(e => console.log(e))
    this.errors.push(errorMessage)
  }

  loadAllTimeData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(tickers).subscribe(report => {
      this.allBarsReport = report
      this.gaps = report.gaps
    }, error => {
      this.handleApiError("Unable to load all bars", error)
    })
  }
}
