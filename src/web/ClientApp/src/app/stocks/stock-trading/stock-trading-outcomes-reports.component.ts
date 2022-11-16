import { Component, Input } from '@angular/core';
import { ThemeService } from 'ng2-charts';
import { StocksService, OutcomesReport, PositionInstance, StockGaps } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
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
  

	constructor(private service : StocksService){}

  @Input()
  dailyMode: boolean = false

  @Input()
  allTimeMode: boolean = false

  @Input()
  positions: PositionInstance[] = []


	ngOnInit(): void {
    
    if (this.allTimeMode) {
      this.loadAllTimeData()
    }

    if (this.dailyMode) {
      this.loadDailyData()
    }
  }
  loadDailyData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarDaily(tickers).subscribe(report => {
      this.singleBarReportDaily = report
      this.loadWeeklyData()
    })
  }

  loadWeeklyData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesSingleBarWeekly(tickers).subscribe(report => {
      this.singleBarReportWeekly = report
      this.loadPositionData()
    })
  }

  loadPositionData() {
    this.service.reportPositions().subscribe(report => {
      this.positionsReport = report
    })
  }

  loadAllTimeData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(tickers).subscribe(report => {
      this.allBarsReport = report
      this.gaps = report.gaps
    })
  }
}
