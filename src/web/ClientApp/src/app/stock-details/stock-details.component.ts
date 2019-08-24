import { Component } from '@angular/core';
import { StocksService, StockSummary } from '../services/stocks.service';
import { ActivatedRoute, Router } from '@angular/router';
import {Location} from '@angular/common';

@Component({
  selector: 'app-home',
  templateUrl: './stock-details.component.html',
  styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent {

	public ticker: string;
	public loaded: boolean = false;
	public stock: StockSummary;
	
	public priceChartType = "ColumnChart";
	public priceOptions = {
		title: "price",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
		}
	};
	public priceChartData: object;

	public volumeChartData: object;
	public volumeOptions = {
		title: "volume",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
		}
	};

	public peChartData: object;
	public peOptions = {
		title: "P/E",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
		}
	};

	public bookChartData: object;
	public bookOptions = {
		title: "book value",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
		}
	};

	constructor(
		private stocks : StocksService,
		private route: ActivatedRoute,
		private router: Router,
		private location : Location){}

	ngOnInit(): void {
		var ticker = this.route.snapshot.paramMap.get('ticker');
		if (ticker){
			this.ticker = ticker;
			this.fetchStock();
		}
	}

	fetchStock() {
		this.stocks.getStockSummary(this.ticker).subscribe(result => {
			this.stock = result;
			this.priceChartData = result.priceChartData;
			this.volumeChartData = result.volumeChartData;
			this.bookChartData = result.bookChartData;
			this.peChartData = result.peChartData;
			this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
		});
	}

	goToDashboard(){
		this.router.navigateByUrl('/dashboard')
	}
}
