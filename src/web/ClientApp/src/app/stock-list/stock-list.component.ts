import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-stock-list',
  templateUrl: './stock-list.component.html',
  styleUrls: ['./stock-list.component.css']
})
export class StockListComponent implements OnInit {

	public loaded: boolean = false;
	public stocks: object;

	constructor(
		private service : StocksService){}

	ngOnInit() {
		this.service.getStocks().subscribe(result => {
			this.stocks = result;
			this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
		});
	}
}
