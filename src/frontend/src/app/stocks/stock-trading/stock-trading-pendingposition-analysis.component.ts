import { Component, OnInit, inject } from '@angular/core';
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";

import {OutcomesAnalysisReportComponent} from "../../shared/reports/outcomes-analysis-report.component";
import {OutcomesReport, StocksService} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";
import { OutcomesComponent } from "../../shared/reports/outcomes.component";

@Component({
    selector: 'app-stock-trading-pendingposition-analysis',
    imports: [
    LoadingComponent,
    ErrorDisplayComponent,
    OutcomesAnalysisReportComponent,
    OutcomesComponent
],
    templateUrl: './stock-trading-pendingposition-analysis.component.html',
    styleUrl: './stock-trading-pendingposition-analysis.component.css'
})
export class StockTradingPendingpositionAnalysisComponent implements OnInit {
    private stockService = inject(StocksService);

    report: OutcomesReport;
    loading = false;
    errors: string[];

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);
    constructor() { }
    
    ngOnInit() {
        this.loading = true;
        this.stockService.reportPendingPositions().subscribe((data) => {
            this.report = data
            this.loading = false;
        }, (error) => {
            this.loading = false;
            this.errors = GetErrors(error)
        })
    }
}
