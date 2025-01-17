import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {StockPosition, StocksService, WeeklyReport} from '../services/stocks.service';
import {GetErrors} from "../services/utils";

@Component({
    selector: 'app-review',
    templateUrl: './summary.component.html',
    styleUrls: ['./summary.component.css'],
    standalone: false
})
export class SummaryComponent implements OnInit {
    result: WeeklyReport
    loaded: boolean = false
    timePeriod: string = 'thisweek'
    errors: string[] = []
    stockProfits: number;
    optionProfits: number;
    dividendProfits: number;

    constructor(
        private stockService: StocksService,
        private title: Title
    ) {
    }

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

