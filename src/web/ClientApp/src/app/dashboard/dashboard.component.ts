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
        this.putCollateral += (o.strikePrice * 100)
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
}
