import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService, OwnedStock } from '../../services/stocks.service';


@Component({
  selector: 'stock-dashboard',
  templateUrl: './stock-dashboard.component.html',
  styleUrls: ['./stock-dashboard.component.css']
})
export class StockDashboardComponent implements OnInit {

	owned : OwnedStock[]
  loaded : boolean = false

  numberOfSharesOwned: number;
  moneySpentOnShares: number;
  currentEquity: number;
  profits: number;

  activeTab: string = 'owned'

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getStocks().subscribe(result => {
      this.owned = result.owned;
      this.loaded = true;
      this.calculateProperties();
      this.sort("profits")
		}, error => {
			console.log(error);
			this.loaded = false;
    })
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

  calculateProperties() {

    this.numberOfSharesOwned = 0.0
    this.moneySpentOnShares = 0.0
    this.currentEquity = 0.0

    for (var i of this.owned) {
      this.numberOfSharesOwned += i.owned
      this.moneySpentOnShares += i.cost
      this.currentEquity += i.equity
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

    this.owned.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    switch(column) {
      case "ticker":
        return (a:OwnedStock, b:OwnedStock) => a.ticker.localeCompare(b.ticker)
      case "currentPrice":
        return (a:OwnedStock, b:OwnedStock) => a.currentPrice - b.currentPrice
      case "averageCost":
        return (a:OwnedStock, b:OwnedStock) => a.averageCost - b.averageCost
      case "owned":
        return (a:OwnedStock, b:OwnedStock) => a.owned - b.owned
      case "equity":
        return (a:OwnedStock, b:OwnedStock) => a.equity - b.equity
      case "profits":
        return (a:OwnedStock, b:OwnedStock) => a.profits - b.profits
      case "profitsPct":
        return (a:OwnedStock, b:OwnedStock) => a.profitsPct - b.profitsPct
    }

    console.log("unrecognized sort column " + column)
    return null;
  }
}
