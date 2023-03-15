import { Component, OnInit } from '@angular/core';
import { StocksService, TransactionsView } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {
  response: TransactionsView
  ticker: string = ""
  groupBy: string = "month"
  filterType: string = ""
  txType: string = "tx"
  loading: boolean = false
  showDetails: string = ''

  constructor(
    private stockService:StocksService,
    private route:ActivatedRoute,
    private title:Title
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
      this.title.setTitle("Transactions - Nightingale Trading")
    }, _ => {
      this.loading = false
    })
  }
}
