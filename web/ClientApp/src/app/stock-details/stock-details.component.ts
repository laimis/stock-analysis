import { Component } from '@angular/core';
import { StocksService, StockSummary } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import {Location} from '@angular/common';

@Component({
  selector: 'app-home',
  templateUrl: './stock-details.component.html',
})
export class StockDetailsComponent {

	public ticker: string;
	public loaded: boolean = false;
	public stock: StockSummary;

	constructor(
		private stocks : StocksService,
		private route: ActivatedRoute,
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

	back(){
		this.location.back();
	}

	buildPriceChartUrl(){
		return this.buildChartUrl(400, 200, 'price', this.stock.priceLabels, this.stock.priceValues, true)
	}

	buildVolumeChartUrl(){
		return this.buildChartUrl(400, 200, 'volumne', this.stock.priceLabels, this.stock.volumeValues, false)
	}

	buildChartUrl(width, height, label, labels, values, datalabel:boolean) {
		var chart:any = {
			type: 'bar',
			data: {
			   labels: labels,
			   datasets: [
				   {
					   label: label,
					   data: values
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
					  display: datalabel,
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

		return "https://quickchart.io/chart?w=" + width + "&h=" + height + "&c="+ payload;
	}
}
