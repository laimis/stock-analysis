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
			this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
		});
	}

	goToDashboard(){
		this.router.navigateByUrl('/dashboard')
	}

	buildPriceChartUrl(){
		var chart:any = {
			type: 'line',
			data: {
			   labels: this.stock.priceLabels,
			   datasets: [
				   {
					   label: 'low',
					   data: this.stock.lowValues
				   },
				   {
					label: 'high',
					data: this.stock.highValues
				}
			   ]
			},
			options: {
				legend: {
					labels: { 
					  fontSize: 8
					}
				},
				plugins: {
					datalabels: {
					  display: true,
					  font: {
						  size: 8
					  }
					},
					axislabels :{
						font: {
							size: 8
						}
					}
				}
			}
		 }

		 var payload = JSON.stringify(chart);

		return "https://quickchart.io/chart?w=500&h=200&c="+ payload;
	}
}
