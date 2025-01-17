import {Component, OnInit} from '@angular/core';
import {StockPosition} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
    selector: 'app-stock-trading-analysis-dashboard',
    templateUrl: './stock-trading-analysis-dashboard.component.html',
    styleUrls: ['./stock-trading-analysis-dashboard.component.css'],
    standalone: false
})
export class StockTradingAnalysisDashboardComponent implements OnInit {

    positions: StockPosition[]
    errors: string[]

    constructor(
        private stocksService: StockPositionsService
    ) {
    }

    ngOnInit() {
        this.stocksService.getTradingEntries().subscribe((data) => {
            this.positions = data.current
        }, (error) => {
            this.errors = GetErrors(error)
        })
    }
}
