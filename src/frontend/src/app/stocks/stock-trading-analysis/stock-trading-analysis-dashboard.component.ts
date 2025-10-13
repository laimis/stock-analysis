import { Component, OnInit, inject } from '@angular/core';
import {PortfolioHoldings, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';

@Component({
    selector: 'app-stock-trading-analysis-dashboard',
    templateUrl: './stock-trading-analysis-dashboard.component.html',
    styleUrls: ['./stock-trading-analysis-dashboard.component.css'],
    standalone: false
})
export class StockTradingAnalysisDashboardComponent implements OnInit {
    private stocksService = inject(StocksService);


    portfolioHoldings: PortfolioHoldings
    errors: string[]

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
    }

    ngOnInit() {
        this.stocksService.getPortfolioHoldings().subscribe((data) => {
            this.portfolioHoldings = data
        }, (error) => {
            this.errors = GetErrors(error)
        })
    }
}
