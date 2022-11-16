import { Component, Input } from '@angular/core';
import { StocksService, StockGaps, OutcomesReport, PositionInstance } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
  templateUrl: './stock-trading-outcomes-reports.component.html',
  styleUrls: ['./stock-trading-outcomes-reports.component.css']
})
export class StockPositionReportsComponent {

  
  sortColumn: string
  sortDirection: number = -1
  allBarsReport: OutcomesReport;
  

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
      // this.loadDailyData()
    }
  }
  loadAllTimeData() {
    var tickers = this.positions.map(p => p.ticker)
    this.service.reportOutcomesAllBars(tickers).subscribe(report => {
      this.allBarsReport = report
    })
  }
}
