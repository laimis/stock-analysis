import { Component, OnInit } from '@angular/core';
import { PositionInstance, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-trading-analysis-dashboard',
  templateUrl: './stock-trading-analysis-dashboard.component.html',
  styleUrls: ['./stock-trading-analysis-dashboard.component.css']
})
export class StockTradingAnalysisDashboardComponent implements OnInit {

  positions : PositionInstance[]
  errors : string[]
  
  constructor(
    private stocksService : StocksService
  ) { }

  ngOnInit() {
    this.stocksService.getTradingEntries().subscribe((data) => {
      this.positions = data.current
    }, (error) => {
      this.errors = GetErrors(error)
    })
  }
}
