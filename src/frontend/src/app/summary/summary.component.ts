import { Component, OnInit, inject } from '@angular/core';
import {Title} from '@angular/platform-browser';
import {StockPosition, StocksService, WeeklyReport} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import { CurrencyPipe, DatePipe, DecimalPipe, NgClass, PercentPipe } from '@angular/common';
import { LoadingComponent } from "../shared/loading/loading.component";
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TradingViewLinkComponent } from "../shared/stocks/trading-view-link.component";
import { StockLinkComponent } from "../shared/stocks/stock-link.component";
import { StockTradingPositionComponent } from "../stocks/stock-trading/stock-trading-position.component";

@Component({
    selector: 'app-review',
    templateUrl: './summary.component.html',
    styleUrls: ['./summary.component.css'],
    imports: [
    DatePipe, CurrencyPipe, LoadingComponent, ErrorDisplayComponent, DecimalPipe, FormsModule, NgClass, TradingViewLinkComponent, StockLinkComponent, PercentPipe,
    StockTradingPositionComponent
]
})
export class SummaryComponent implements OnInit {
    private stockService = inject(StocksService);
    private title = inject(Title);

    result: WeeklyReport
    loaded: boolean = false
    timePeriod: string = 'thisweek'
    errors: string[] = []
    stockProfits: number;
    optionProfits: number;
    dividendProfits: number;

    ngOnInit() {
        this.title.setTitle("Weekly Summary - Nightingale Trading")
        this.loadEntries()
    }

    getStrategy(p: StockPosition) {
        return p.labels.find(l => l.key === 'strategy')?.value
    }

    periodChanged() {
        this.loadEntries()
    }

    stockTransactionTotal(): number {
        return this.result.stockTransactions.reduce((acc, cur) => acc + cur.amount, 0)
    }

    closedPositionProfit(): number {
        return this.result.closedStocks.reduce((acc, cur) => acc + cur.profit, 0)
    }

    closedPositionRR(): number {
        return this.result.closedStocks.reduce((acc, cur) => acc + cur.rr, 0)
    }

    private loadEntries() {
        this.stockService.reportsWeeklySummary(this.timePeriod).subscribe((r: WeeklyReport) => {
            this.loaded = true
            this.result = r
            this.stockProfits = r.plStockTransactions.reduce((acc, cur) => acc + cur.profit, 0)
            this.optionProfits = r.closedOptions.reduce((acc, cur) => acc + cur.profit, 0) * 100
            this.dividendProfits = r.dividends.reduce((acc, cur) => acc + cur.netAmount, 0)
        }, error => {
            this.errors = GetErrors(error)
            this.loaded = true
        })
    }
}

