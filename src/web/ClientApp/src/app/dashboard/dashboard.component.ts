import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public owned : object[];
  public openOptions : object[];
  public pendingPremium : number;
  public optionEarnings : number;
	public collateralShares : number;
  public collateralCash : number;
  public ticker : string;
	public loaded : boolean = false;

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getPortfolio().subscribe(result => {
      this.owned = result.owned;
      this.openOptions = result.openOptions;
      this.pendingPremium = result.pendingPremium;
      this.optionEarnings = result.optionEarnings;
      this.collateralCash = result.collateralCash;
      this.collateralShares = result.collateralShares;
			this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = false;
		})
	}

	goToStock() {
		this.router.navigateByUrl('/options/' + this.ticker)
	}

}
