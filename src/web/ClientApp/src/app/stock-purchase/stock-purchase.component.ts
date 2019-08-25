import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import {Location} from '@angular/common';

@Component({
  selector: 'app-stock-purchase',
  templateUrl: './stock-purchase.component.html',
  styleUrls: ['./stock-purchase.component.css']
})
export class StockPurchaseComponent implements OnInit {

	public ticker: string
	public amount: Number
	public price: Number
	public date: Date
	public purchased: Boolean

	constructor(
		private service : StocksService,
		private location : Location){}

	ngOnInit() {
	}

	submitPurchase() {

		this.purchased = false;

		this.service.purchase(this.toObject()).subscribe(() => {
			this.purchased = true;
			this.clearValues()
		})
	}

	submitSell() {

		this.purchased = false;

		this.service.sell(this.toObject()).subscribe(() => {
			this.purchased = true;
			this.clearValues()
		})
	}

	toObject() {
		return {
			ticker:this.ticker,
			amount:this.amount,
			price:this.price,
			date:this.date
		}
	}

	clearValues() {
		this.ticker = null
		this.price = null
		this.date = null
		this.amount = null
	}

	back(){
		this.location.back();
	}
}
