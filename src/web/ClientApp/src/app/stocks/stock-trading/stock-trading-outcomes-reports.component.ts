import { Component, Input, OnInit } from '@angular/core';
import { GetErrors } from 'src/app/services/utils';
import { StocksService, OutcomesReport, PositionInstance, StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
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
    this.service.reportOutcomesSingleBarDaily(tickers).subscribe(report => {
      this.singleBarReportDaily = report
    }, error => {
      this.handleApiError(error)
    },
    () => {
      this.loadWeeklyData()
    }
    )
  }

  loadWeeklyData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarWeekly(tickers).subscribe(report => {
      this.singleBarReportWeekly = report
    }, error => {
      this.handleApiError(error)
    })

  }

  loadPositionData() {
    this.service.reportPositions().subscribe(report => {
      this.positionsReport = report
    }, error => {
      this.handleApiError(error)
    },
    () => {
      this.loadDailyData()
    })
  }

  private handleApiError(error: any) {
    this.errors.push("Unable to load position reports.")
      var forConsole = GetErrors(error)
      forConsole.forEach(e => console.log(e))
  }

  loadAllTimeData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(tickers).subscribe(report => {
      this.allBarsReport = report
      this.gaps = report.gaps
    }, error => {
      this.handleApiError(error)
    })
  }
}
