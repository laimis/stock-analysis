import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { StocksService, StockTradingPosition, StockTradingPositions, StockTradingPerformanceCollection, BrokerageOrder } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading',
  templateUrl: './stock-trading-dashboard.component.html',
  styleUrls: ['./stock-trading-dashboard.component.css']
})
export class StockTradingComponent implements OnInit {
  positions: StockTradingPosition[]
  past: StockTradingPosition[]
  brokerageOrders: BrokerageOrder[]
  loaded: boolean = false
  loading: boolean = true
  activeTab:string = 'positions'
  performance: StockTradingPerformanceCollection;

  constructor(
    private stockService:StocksService,
    private title: Title,
    private route: ActivatedRoute
    ) { }

  ngOnInit() {
    this.activeTab = this.route.snapshot.paramMap.get('tab') || 'positions'
    this.title.setTitle("Trading Dashboard - Nightingale Trading")
    this.loadEntries()
  }

  
  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }

  stockPurchased() {
    this.loadEntries()
    this.activateTab('positions')
  }

  brokerageOrderEntered() {
    this.loadEntries()
    this.activateTab('brokerage')
  }

  orderCancelled() {
    this.loadEntries()
  }

  refresh() {
    this.loadEntries()
  }

  totalCost() {
    return this.positions.reduce((acc, cur) => acc + (cur.averageCostPerShare * cur.numberOfShares), 0)
  }

  private loadEntries() {
    this.loading = true
    this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
      this.positions = r.current
      this.past = r.past
      this.performance = r.performance
      this.brokerageOrders = r.brokerageOrders
      this.loading = false
      this.loaded = true
    }, _ => {
      this.loading = false
      this.loaded = true
    })
  }

  numberOfPositions: number = 0
  invested: number = 0
}

