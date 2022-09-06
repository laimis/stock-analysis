import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { HideIfHidden, StocksService, OwnedStock, StockTradingPosition, BrokerageOrder, StockViolation } from '../../services/stocks.service';

@Component({
  selector: 'stock-dashboard',
  templateUrl: './stock-dashboard.component.html',
  styleUrls: ['./stock-dashboard.component.css']
})
export class StockDashboardComponent implements OnInit {

  violations: StockViolation[] = []
  positions : StockTradingPosition[]
  orders : BrokerageOrder[]
  loaded : boolean = false

  numberOfSharesOwned: number;
  moneySpentOnShares: number;
  currentEquity: number;
  profits: number;

  activeTab: string = 'owned'

  selectedCategory: string = 'all'

	constructor(
		private stocks : StocksService,
    private router : Router,
    private title : Title)
	{ }

	ngOnInit() {

    this.title.setTitle("Stocks - Nightingale Trading")

		this.fetchData()
	}

  fetchData() {
    this.stocks.getStocks().subscribe(result => {
      this.positions = result.positions
      this.violations = result.violations
      this.orders = result.orders
      this.loaded = true
      this.calculateProperties()
      this.sort("profits")
		}, error => {
			console.log(error);
			this.loaded = false;
    })
  }

  hideIfHidden(value : number) : number {
    return HideIfHidden(value, true)
  }

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }

  categoryChanged() {
    this.calculateProperties()
  }

  calculateProperties() {

    this.numberOfSharesOwned = 0.0
    this.moneySpentOnShares = 0.0
    this.currentEquity = 0.0

    for (var p of this.positions) {

      if (this.selectedCategory == "longterm" || this.selectedCategory == "shortterm") {
        console.log('doing comparison of ' + p.category + ' vs ' + this.selectedCategory)
        if (p.category != this.selectedCategory) {
          continue
        }
      }

      if (this.selectedCategory == "notset") {
        if (p.category != null) {
          continue
        }
      }

      this.numberOfSharesOwned += p.numberOfShares
      this.moneySpentOnShares += p.cost
      this.currentEquity += p.cost + p.profit
    }

    this.profits = 0.0
    if (this.moneySpentOnShares != 0) {
      var made = this.currentEquity - this.moneySpentOnShares
      this.profits = made / this.moneySpentOnShares
    }
  }

  sortColumn : string
  sortDirection : number = -1

  sort(column:string) {

    var func = this.getSortFunc(column);

    if (this.sortColumn != column) {
      this.sortDirection = -1
    } else {
      this.sortDirection *= -1
    }
    this.sortColumn = column

    var finalFunc = (a, b) => {
      var result = func(a, b)
      return result * this.sortDirection
    }

    this.positions.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    switch(column) {
      case "ticker":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.ticker.localeCompare(b.ticker)
      case "price":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.price - b.price
      case "averageCost":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.averageCost - b.averageCost
      case "owned":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.numberOfShares - b.numberOfShares
      case "equity":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.cost + a.unrealizedGain - (b.cost + b.unrealizedGain)
      case "profits":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.unrealizedGain - b.unrealizedGain
      case "profitsPct":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.unrealizedGainPct - b.unrealizedGainPct
    }

    console.log("unrecognized sort column " + column)
    return null;
  }
}
