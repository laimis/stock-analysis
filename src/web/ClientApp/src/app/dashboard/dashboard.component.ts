import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService, OwnedStock, OwnedOption } from '../services/stocks.service';


@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public owned : OwnedStock[];
  public openOptions : OwnedOption[];
  public loaded : boolean = false;

  numberOfSharesOwned: number;
  moneySpentOnShares: number;
  currentEquity: number;
  profits: number;
  optionPremium: number;
  putContracts: number;
  callContracts: number;
  putCollateral: number;
  putPremiumvsCollateralPct: number;

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getPortfolio().subscribe(result => {
      this.owned = result.owned;
      this.openOptions = result.openOptions;
      this.loaded = true;
      this.calculateProperties();
		}, error => {
			console.log(error);
			this.loaded = false;
    })
	}

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
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

    this.optionPremium = 0.0
    this.putContracts = 0
    this.callContracts = 0
    this.putCollateral = 0
    var putPremium = 0
    for (var o of this.openOptions) {
      this.optionPremium += o.premium
      if (o.optionType == "PUT")
      {
        this.putContracts++
        putPremium += o.premium

        if (o.boughtOrSold == 'Sold')
        {
          this.putCollateral += (o.strikePrice * 100)
        }
      }

      if (o.optionType == "CALL")
      {
        this.callContracts++
      }
    }
    this.putPremiumvsCollateralPct = 0
    if (putPremium > 0)
    {
      this.putPremiumvsCollateralPct = putPremium / (1.0 * this.putCollateral)
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
