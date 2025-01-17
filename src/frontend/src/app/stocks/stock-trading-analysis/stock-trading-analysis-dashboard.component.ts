import {Component, OnInit} from '@angular/core';
import {PortfolioHoldings, StockPosition, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {isQuestionOrPlusOrMinusToken} from "typescript";

@Component({
    selector: 'app-stock-trading-analysis-dashboard',
    templateUrl: './stock-trading-analysis-dashboard.component.html',
    styleUrls: ['./stock-trading-analysis-dashboard.component.css'],
    standalone: false
})
export class StockTradingAnalysisDashboardComponent implements OnInit {

    portfolioHoldings: PortfolioHoldings
    errors: string[]

    constructor(
        private stocksService: StocksService
    ) {
    }

    ngOnInit() {
        this.stocksService.getPortfolioHoldings().subscribe((data) => {
            this.portfolioHoldings = data
        }, (error) => {
            this.errors = GetErrors(error)
        })
    }

    protected readonly isQuestionOrPlusOrMinusToken = isQuestionOrPlusOrMinusToken;
}
