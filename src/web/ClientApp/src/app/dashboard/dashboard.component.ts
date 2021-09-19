import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { StocksService, Dashboard } from '../services/stocks.service';


@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

	dashboard : Dashboard
  loaded : boolean = false

	constructor(
		private stocks : StocksService,
    private router : Router,
    private title: Title)
	{ }

	ngOnInit() {

    this.title.setTitle("Dashboard - Nightingale Trading")

		this.stocks.getPortfolio().subscribe(result => {
      this.dashboard = result;
      this.loaded = true;
		}, error => {
			console.log(error);
			this.loaded = false;
    })
	}

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }
}
