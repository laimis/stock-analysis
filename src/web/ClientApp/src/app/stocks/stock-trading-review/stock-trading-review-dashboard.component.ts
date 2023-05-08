import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PositionInstance, StocksService, StockTradingPerformanceCollection, TradingStrategyPerformance } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-trading-review-dashboard',
  templateUrl: './stock-trading-review-dashboard.component.html',
  styleUrls: ['./stock-trading-review-dashboard.component.css']
})
export class StockTradingReviewDashboardComponent implements OnInit {
  activeTab: string = 'positions';
  loaded = false;
  loading = true;
  past: PositionInstance[];
  performance: StockTradingPerformanceCollection;
  strategies: TradingStrategyPerformance[]

  constructor(
    private route: ActivatedRoute,
    private stockService: StocksService
  ) { }

  ngOnInit() {
    this.loadEntries()
    this.activeTab = this.route.snapshot.paramMap.get('tab') || 'positions'
  }

  loadEntries() {
    this.loading = true
    this.stockService.getTradingEntries().subscribe(
      response => {
        this.past = response.past
        this.performance = response.performance
        this.strategies = response.strategyPerformance
        this.loading = false
        this.loaded = true
      }, _ => {
        this.loading = false
        this.loaded = true
      },
      () => {
        this.loading = false
        this.loaded = true
      }
    )
  }

  refresh() {
    this.loadEntries()
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }
}
