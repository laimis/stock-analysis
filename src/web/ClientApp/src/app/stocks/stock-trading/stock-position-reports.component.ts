import { Component, Input } from '@angular/core';
import { StocksService, StockGaps, Evaluations } from '../../services/stocks.service';

@Component({
  selector: 'app-position-reports',
  templateUrl: './stock-position-reports.component.html',
  styleUrls: ['./stock-position-reports.component.css']
})
export class StockPositionReportsComponent {

  
  sortColumn: string
  sortDirection: number = -1
  

	constructor(private service : StocksService){}

  @Input()
  dailyMode: boolean = false

  @Input()
  allTimeMode: boolean = false


	ngOnInit(): void {
    
    if (this.allTimeMode) {
      // this.loadAllTimeData()
    }

    if (this.dailyMode) {
      // this.loadDailyData()
    }
  }
}
