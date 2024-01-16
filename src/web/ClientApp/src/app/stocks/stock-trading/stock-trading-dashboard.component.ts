import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute} from '@angular/router';
import {
  BrokerageAccount,
  PositionInstance, StockQuote,
  StockTradingPositions, stocktransactioncommand,
  StockViolation
} from '../../services/stocks.service';
import {GetErrors} from "../../services/utils";
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
  selector: 'app-stock-trading-dashboard',
  templateUrl: './stock-trading-dashboard.component.html',
  styleUrls: ['./stock-trading-dashboard.component.css']
})
export class StockTradingDashboardComponent implements OnInit {
  positions: PositionInstance[]
  loaded: boolean = false
  loading: boolean = true
  activeTab:string = 'positions'
  violations: StockViolation[]
  brokerageAccount: BrokerageAccount
  errors: string[]
  quotes: Map<string, StockQuote>

  constructor(
    private stockService:StockPositionsService,
    private title: Title,
    private route: ActivatedRoute
    ) { }

  ngOnInit() {
    this.route.params.subscribe(param => {
      this.activeTab = param['tab'] || 'positions'
    })

    this.title.setTitle("Trading Dashboard - Nightingale Trading")
    this.loadEntries()
  }


  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }

  sellRequested(command:stocktransactioncommand) {
    this.stockService.sell(command).subscribe(
      () => {
        this.loadEntries()
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  purchaseRequested(command:stocktransactioncommand) {
    this.stockService.purchase(command).subscribe(
      () => {
        this.loadEntries()
      },
      error => {
        this.errors = GetErrors(error)
      }
    )
  }

  refresh() {
    this.loadEntries()
  }

  private loadEntries() {
    this.loading = true
    this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
      this.positions = r.current
      this.violations = r.violations
      this.brokerageAccount = r.brokerageAccount
      this.quotes = r.prices
      this.loading = false
      this.loaded = true
    }, err => {
      this.loading = false
      this.loaded = true
      console.log(err)
      this.errors = GetErrors(err)
    })
  }

  invested: number = 0
}

