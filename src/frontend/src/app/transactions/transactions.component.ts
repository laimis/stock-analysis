import {Component, OnInit} from '@angular/core';
import {DataPointContainer, StocksService, TransactionsView} from '../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {GetErrors} from "../services/utils";

@Component({
    selector: 'app-transactions',
    templateUrl: './transactions.component.html',
    styleUrls: ['./transactions.component.css'],
    standalone: false
})
export class TransactionsComponent implements OnInit {
    response: TransactionsView
    ticker: string = ""
    groupBy: string = "month"
    filterType: string = ""
    txType: string = "pl"
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
        this.stockService.reportTransactions(this.ticker, this.groupBy, this.filterType, this.txType).subscribe(r => {
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
    
    // pl breakdown chart toggling
    showCharts = false;
    selectedChartType = 'all';
    filteredCharts: DataPointContainer[] = [];

    onChartTypeChange(selectedLabel: string) {
        // Assuming your DataPointContainer has some identifier in the label
        // You might need to adjust this logic based on your actual data structure
        this.filteredCharts = this.response.plBreakdowns.filter(chart => chart.label === selectedLabel);
    }
}
