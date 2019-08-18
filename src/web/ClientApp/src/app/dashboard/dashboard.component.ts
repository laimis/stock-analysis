import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public tickers : string[];
	public ticker : string;
	public dashboard : object;
	public loaded : boolean = false;

	constructor(
		private stocks : StocksService,
		private router : Router)
	{ }

	ngOnInit() {
		this.tickers = [
			'F',
			'CTST',
			'EGBN',
			'BAC',
			'ACB',
			'IRBT',
			'TRQ',
			'TEUM',
			'FTSV'
		];

		this.stocks.getDashboard().subscribe(result => {
			this.dashboard = result;
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
