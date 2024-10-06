import {Component, OnInit} from '@angular/core';
import {StocksService, TransactionsView} from '../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {GetErrors} from "../services/utils";

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
    loading: boolean = true
    showDetails: string = ''
    errors: string[] = []

    constructor(
        private stockService: StocksService,
        private route: ActivatedRoute,
        private title: Title
    ) {
    }

    ngOnInit() {
        const ticker = this.route.snapshot.queryParamMap.get("ticker");
        if (ticker) {
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
        }, error => {
            this.errors = GetErrors(error)
            this.loading = false
        })
    }
    
    breakdownSelected(groupBy: string) {
        this.groupBy = groupBy
        this.loadData()
    }

    filterTypeSelected(filterType: string) {
        this.filterType = filterType
        this.loadData()
    }
    
    tickerSelected(ticker: string) {
        this.ticker = ticker
        this.loadData()
    }

    txTypeSelected(txType: string) {
        this.txType = txType
        this.loadData()
    }
}
