import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {
  transactions: object[]
  ticker: string;

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute,
  ) { }

  ngOnInit() {
    this.ticker = this.route.snapshot.paramMap.get("ticker")
    this.loadTransactions(this.ticker)
  }

  loadTransactions(ticker:string) {
    this.stockService.getTransactions(ticker).subscribe(r => {
      this.transactions = r
    })
  }

}
