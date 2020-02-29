import { Component, Input } from '@angular/core';
import { StockSummary } from '../services/stocks.service';

@Component({
  selector: 'stock-charts',
  templateUrl: './stock-charts.component.html',
  styleUrls: ['./stock-charts.component.css']
})

export class StockChartsComponent {

  public summary : StockSummary;

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

	public bookChartData: object
	public bookOptions = {
		title: "book value",
		legend: { position: "none" },
		bar: {
			groupWidth: 15
		}
  };

  @Input()
  set stock(stock: StockSummary) {
    this.priceChartData = stock.priceChartData;
    this.volumeChartData = stock.volumeChartData;
    this.bookChartData = stock.bookChartData;
    this.peChartData = stock.peChartData;
    this.summary = stock
  }
  get stock(): StockSummary { return this.summary; }

	constructor(){}

	ngOnInit(): void {}
}
