import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	public tickers : string[];
	public ticker : string;

	constructor(private router : Router) { }

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
	}

	goToStock() {
		this.router.navigateByUrl('/stocks/' + this.ticker)
	}

}
