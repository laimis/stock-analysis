import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import {DatePipe} from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-stock-transaction',
  templateUrl: './stock-transaction.component.html',
  styleUrls: ['./stock-transaction.component.css'],
  providers: [DatePipe]
})
export class StockTransactionComponent implements OnInit {

	public ticker: string
	public amount: Number
	public price: Number
	public date: String
	public purchased: Boolean

	constructor(
		private service : StocksService,
		private route: ActivatedRoute,
		private datePipe: DatePipe){}

	ngOnInit() {
		var ticker = this.route.snapshot.paramMap.get('ticker');
		if (ticker){
			this.ticker = ticker;
		}

		this.date = Date()
		this.date = this.datePipe.transform(this.date, 'yyyy-MM-dd');
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
}
