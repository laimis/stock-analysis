import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public portfolioTickers : string[];
	public ticker : string;
	public loaded : boolean = false;

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {

		this.stocks.getPortfolio().subscribe(result => {
			this.portfolioTickers = result;
			this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = true;
		})
	}

	goToStock() {
		this.router.navigateByUrl('/stocks/' + this.ticker)
	}

}
