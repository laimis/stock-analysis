import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import {Location} from '@angular/common';

@Component({
  selector: 'app-stock-list',
  templateUrl: './stock-list.component.html',
  styleUrls: ['./stock-list.component.css']
})
export class StockListComponent implements OnInit {

	public loaded: boolean = false;
	public stocks: object;

	constructor(
		private service : StocksService,
		private location : Location){}

	ngOnInit() {
		this.service.getStocks().subscribe(result => {
			this.stocks = result;
			this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
		});
	}

	back(){
		this.location.back();
	}
}
