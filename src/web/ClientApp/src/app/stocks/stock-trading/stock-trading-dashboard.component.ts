import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { StocksService, PositionInstance, StockTradingPositions, StockTradingPerformanceCollection, BrokerageOrder, StockViolation } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading',
  templateUrl: './stock-trading-dashboard.component.html',
  styleUrls: ['./stock-trading-dashboard.component.css']
})
export class StockTradingComponent implements OnInit {
  positions: PositionInstance[]
  closed: PositionInstance[]
  loaded: boolean = false
  loading: boolean = true
  activeTab:string = 'positions'
  performance: StockTradingPerformanceCollection;
  violations: StockViolation[]

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

  orderExecuted() {
    this.loadEntries()
  }

  refresh() {
    this.loadEntries()
  }

  totalCost() {
    return this.positions.reduce((acc, cur) => acc + (cur.averageCostPerShare * cur.numberOfShares), 0)
  }

  shortTermCount() {
    return this.positions.filter(p => p.isShortTerm).length
  }

  totalRiskedAmount() {
    return this.positions.reduce((acc, cur) => acc + cur.costAtRiskedBasedOnStopPrice, 0)
  }

  private loadEntries() {
    this.loading = true
    this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
      this.positions = r.current
      this.closed = r.past
      this.performance = r.performance
      this.violations = r.violations
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

