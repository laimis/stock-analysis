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
  ticker: string
  groupBy: string

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute,
  ) { }

  ngOnInit() {
    var ticker = this.route.snapshot.queryParamMap.get("ticker")

    this.groupBy = "month"
    this.ticker = ticker;

    this.loadData()
  }

  loadData() {
    this.stockService.getTransactions(this.ticker, this.groupBy).subscribe(r => {
      this.response = r
    })
  }

  setTicker(ticker:string) {
    this.ticker = ticker
    this.loadData()
  }

  clearFilter() {
    this.ticker = null
    this.loadData()
  }

  setGroupBy(type:string) {
    if (type != this.groupBy)
    {
      this.groupBy = type
      this.loadData()
    }
  }

}
