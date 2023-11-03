import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute} from '@angular/router';
import {
  BrokerageOrder,
  PositionInstance,
  StocksService,
  StockTradingPositions,
  StockViolation
} from '../../services/stocks.service';
import {GetErrors} from "../../services/utils";

@Component({
  selector: 'app-stock-trading-dashboard',
  templateUrl: './stock-trading-dashboard.component.html',
  styleUrls: ['./stock-trading-dashboard.component.css']
})
export class StockTradingComponent implements OnInit {
  positions: PositionInstance[]
  loaded: boolean = false
  loading: boolean = true
  activeTab:string = 'positions'
  violations: StockViolation[]
  brokerageOrders: BrokerageOrder[];
  cashBalance: number;
  errors: string[];

  constructor(
    private stockService:StocksService,
    private title: Title,
    private route: ActivatedRoute
    ) { }

  ngOnInit() {
    this.route.params.subscribe(param => {
      this.activeTab = param['tab'] || 'positions'
      this.title.setTitle("Trading Dashboard - Nightingale Trading")
      this.loadEntries()
    })
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

  orderExecuted() {
    this.loadEntries()
  }

  refresh() {
    this.loadEntries()
  }

  private loadEntries() {
    this.loading = true
    this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
      this.positions = r.current
      this.violations = r.violations
      this.brokerageOrders = r.brokerageOrders
      this.cashBalance = r.cashBalance
      this.loading = false
      this.loaded = true
    }, err => {
      this.loading = false
      this.loaded = true
      console.log(err)
      this.errors = GetErrors(err)
    })
  }

  numberOfPositions: number = 0
  invested: number = 0
}

