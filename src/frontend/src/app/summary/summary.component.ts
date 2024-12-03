import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {PositionInstance, ReviewList, StocksService} from '../services/stocks.service';
import {GetErrors} from "../services/utils";

@Component({
    selector: 'app-review',
    templateUrl: './summary.component.html',
    styleUrls: ['./summary.component.css'],
    standalone: false
})
export class SummaryComponent implements OnInit {
    result: ReviewList
    loaded: boolean = false
    timePeriod: string = 'thisweek'
    errors: string[] = []

    constructor(
        private stockService: StocksService,
        private title: Title
    ) {
    }

    ngOnInit() {
        this.title.setTitle("Weekly Summary - Nightingale Trading")
        this.loadEntries()
    }

    getStrategy(p: PositionInstance) {
        return p.labels.find(l => l.key === 'strategy')?.value
    }

    periodChanged() {
        this.loadEntries()
    }

    stockTransactionTotal(): number {
        return this.result.stockTransactions.reduce((acc, cur) => acc + cur.amount, 0)
    }

    closedPositionProfit(): number {
        return this.result.closedPositions.reduce((acc, cur) => acc + cur.profit, 0)
    }

    closedPositionRR(): number {
        return this.result.closedPositions.reduce((acc, cur) => acc + cur.rr, 0)
    }

    private loadEntries() {
        this.stockService.getTransactionSummary(this.timePeriod).subscribe((r: ReviewList) => {
            this.loaded = true
            this.result = r
        }, error => {
            this.errors = GetErrors(error)
            this.loaded = true
        })
    }
}

