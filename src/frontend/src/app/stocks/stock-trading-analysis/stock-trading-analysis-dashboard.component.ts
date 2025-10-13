import { Component, OnInit, inject } from '@angular/core';
import {PortfolioHoldings, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import { ErrorDisplayComponent } from "src/app/shared/error-display/error-display.component";
import { LoadingComponent } from "src/app/shared/loading/loading.component";
import { StockPositionReportsComponent } from "src/app/stocks/stock-trading/stock-trading-outcomes-reports.component";

@Component({
    selector: 'app-stock-trading-analysis-dashboard',
    templateUrl: './stock-trading-analysis-dashboard.component.html',
    styleUrls: ['./stock-trading-analysis-dashboard.component.css'],
    standalone: true,
    imports: [ErrorDisplayComponent, LoadingComponent, StockPositionReportsComponent]
})
export class StockTradingAnalysisDashboardComponent implements OnInit {
    private stocksService = inject(StocksService);


    portfolioHoldings: PortfolioHoldings
    errors: string[]

    ngOnInit() {
        this.stocksService.getPortfolioHoldings().subscribe((data) => {
            this.portfolioHoldings = data
        }, (error) => {
            this.errors = GetErrors(error)
        })
    }
}
