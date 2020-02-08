import { Component } from '@angular/core';
import { StocksService, StockSummary } from '../services/stocks.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './stock-details.component.html',
  styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent {

	public ticker: string;
	public loaded: boolean = false;
  public stock: StockSummary;
  public profile: object;
  public stats: object;

	public priceChartType = "LineChart";
	public priceOptions = {
		title: "price",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
    },
    vAxis: {
      format: "currency",
      baseline: 0
    }
	};
	public priceChartData: object;

  public volumeChartType = "ColumnChart";
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
		private router: Router){}

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
      this.profile = result.profile;
      this.stats = result.stats;
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
}
