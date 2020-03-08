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
  ticker: string = ""
  groupBy: string = "month"
  filterType: string = ""
  txType: string = "tx"
  loading: boolean = false

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute,
  ) { }

  ngOnInit() {
    var ticker = this.route.snapshot.queryParamMap.get("ticker")
    if (ticker)
    {
      this.ticker = ticker;
    }
    this.loadData()
  }

  loadData() {
    this.loading = true
    this.stockService.getTransactions(this.ticker, this.groupBy, this.filterType, this.txType).subscribe(r => {
      this.response = r
      this.loading = false
    }, _ => {
      this.loading = false
    })
  }
}
