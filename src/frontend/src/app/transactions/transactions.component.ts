import { Component, OnInit, inject } from '@angular/core';
import {DataPointContainer, StocksService, TransactionsView} from '../services/stocks.service';
import {ActivatedRoute, RouterModule} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {GetErrors} from "../services/utils";
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { FormsModule } from '@angular/forms';
import { LoadingComponent } from "../shared/loading/loading.component";
import { LineChartComponent } from "../shared/line-chart/line-chart.component";
import { StockLinkComponent } from "../shared/stocks/stock-link.component";

@Component({
    selector: 'app-transactions',
    imports: [CurrencyPipe, ErrorDisplayComponent, FormsModule, LoadingComponent, LineChartComponent, StockLinkComponent, RouterModule, DatePipe],
    templateUrl: './transactions.component.html',
    styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent implements OnInit {
    private stockService = inject(StocksService);
    private route = inject(ActivatedRoute);
    private title = inject(Title);

    response: TransactionsView
    ticker: string = ""
    groupBy: string = "month"
    filterType: string = ""
    txType: string = "pl"
    loading: boolean = true
    showDetails: string = ''
    errors: string[] = []

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
    
    breakdownSelected(groupBy: any) {
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
