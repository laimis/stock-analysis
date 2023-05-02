import { Component, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';
import { StocksService, PositionInstance, StockTradingPositions, StockTradingPerformanceCollection, BrokerageOrder, StockViolation } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-dashboard',
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
  brokerageOrders: BrokerageOrder[];

  @ViewChild(BrokerageOrdersComponent) brokerageOrdersComponent:BrokerageOrdersComponent;
  positionGroups: any[];

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
    this.activateTab('brokerage')
    this.brokerageOrdersComponent.refreshOrders()
  }

  orderExecuted() {
    this.loadEntries()
  }

  refresh() {
    this.loadEntries()
  }

  totalCost(positions) {
    return positions.reduce((acc, cur) => acc + (cur.averageCostPerShare * cur.numberOfShares), 0)
  }

  breakdownByLabel(positions) : {label:string, positions:PositionInstance[]}[] {
    console.log("breaking down positions")

    if (!positions) return []

    // get unique "strategy" labels
    let uniqueStrategies = positions.reduce((acc, cur) => {
      let strategy = cur.labels.find(l => l.key == 'strategy')
      let strategyKey = strategy ? strategy.value : "none"
      
      if (!acc[strategyKey]) {
        acc[strategyKey] = []
      }
      
      acc[strategyKey].push(cur)
      
      return acc
    }, {})

    return uniqueStrategies
  }

  totalRiskedAmount(positions) {
    return positions.reduce((acc, cur) => acc + cur.costAtRiskedBasedOnStopPrice, 0)
  }

  profit(positions) {
    return positions.reduce((acc, cur) => acc + cur.combinedProfit, 0)
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
      this.positionGroups = this.breakdownByLabel(this.positions)

      this.loadBrokerageOrders()
    }, _ => {
      this.loading = false
      this.loaded = true
    })
  }
  loadBrokerageOrders() {
    this.stockService.brokerageOrders().subscribe((r: BrokerageOrder[]) => {
      this.brokerageOrders = r
    })
  }

  numberOfPositions: number = 0
  invested: number = 0
}

