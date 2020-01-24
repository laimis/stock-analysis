import { Component, OnInit } from '@angular/core';
import { StocksService, TransactionList } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {
  response: TransactionList
  ticker: string;

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute,
  ) { }

  ngOnInit() {
    var ticker = this.route.snapshot.queryParamMap.get("ticker")
    this.loadTransactions(ticker)
  }

  loadTransactions(ticker:string) {
    this.ticker = ticker
    this.stockService.getTransactions(ticker).subscribe(r => {
      this.response = r
    })
  }

  clearFilter() {
    this.loadTransactions(null)
  }

}
