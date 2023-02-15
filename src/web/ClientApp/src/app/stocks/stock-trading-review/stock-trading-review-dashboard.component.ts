import { Component, OnInit } from '@angular/core';
import { PositionInstance, StocksService, StockTradingPerformanceCollection } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-trading-review-dashboard',
  templateUrl: './stock-trading-review-dashboard.component.html',
  styleUrls: ['./stock-trading-review-dashboard.component.css']
})
export class StockTradingReviewDashboardComponent implements OnInit {
  activeTab: string = 'positions';
  loaded = false;
  past: PositionInstance[];
  performance: StockTradingPerformanceCollection;

  constructor(
    private stockService: StocksService
  ) { }

  ngOnInit() {
    this.loadEntries()
  }

  loadEntries() {
    this.stockService.getTradingEntries().subscribe(
      response => {
        this.past = response.past
        this.performance = response.performance
        this.loaded = true
      }
    )
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }
}
