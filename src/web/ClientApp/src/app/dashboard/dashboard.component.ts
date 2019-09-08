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
	public cashedOut : object[];
	public totalSpent : Number;
	public totalEarned : Number;
	public totalCashedOutSpend : number;
	public totalCashedOutEarnings : number;
	public ticker : string;
	public loaded : boolean = false;

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getPortfolio().subscribe(result => {
			this.owned = result.owned;
			this.cashedOut = result.cashedOut;
			this.totalEarned = result.totalEarned;
			this.totalSpent = result.totalSpent;
			this.totalCashedOutSpend = result.totalCashedOutSpend;
			this.totalCashedOutEarnings = result.totalCashedOutEarnings;
			this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = true;
		})
	}

	goToStock() {
		this.router.navigateByUrl('/stocks/' + this.ticker)
	}

	cashedOutGains(){
		return this.totalCashedOutEarnings - this.totalCashedOutSpend;
	}

	cashedOutGainsPct(){
		return this.cashedOutGains() / this.totalCashedOutSpend * 100;
	}

}
