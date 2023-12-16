import { Component, OnInit } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
  selector: 'app-stock-trading-analysis-dashboard',
  templateUrl: './stock-trading-analysis-dashboard.component.html',
  styleUrls: ['./stock-trading-analysis-dashboard.component.css']
})
export class StockTradingAnalysisDashboardComponent implements OnInit {

  positions : PositionInstance[]
  errors : string[]

  constructor(
    private stocksService : StockPositionsService
  ) { }

  ngOnInit() {
    this.stocksService.getTradingEntries().subscribe((data) => {
      this.positions = data.current
    }, (error) => {
      this.errors = GetErrors(error)
    })
  }
}
