import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {
  transactions: object[]

  constructor(
    private stockService:StocksService
  ) { }

  ngOnInit() {
    this.loadTransactions()
  }

  loadTransactions() {
    this.stockService.getTransactions().subscribe(r => {
      this.transactions = r
    })
  }

}
