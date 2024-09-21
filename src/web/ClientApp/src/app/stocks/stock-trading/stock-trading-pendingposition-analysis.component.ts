import {Component, OnInit} from '@angular/core';
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {NgIf} from "@angular/common";
import {OutcomesAnalysisReportComponent} from "../../shared/reports/outcomes-analysis-report.component";
import {OutcomesReport, StocksService} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";

@Component({
  selector: 'app-stock-trading-pendingposition-analysis',
  standalone: true,
    imports: [
        LoadingComponent,
        ErrorDisplayComponent,
        NgIf,
        OutcomesAnalysisReportComponent
    ],
  templateUrl: './stock-trading-pendingposition-analysis.component.html',
  styleUrl: './stock-trading-pendingposition-analysis.component.css'
})
export class StockTradingPendingpositionAnalysisComponent implements OnInit {
    report: OutcomesReport;
    loading = false;
    errors: string[];
    constructor(private stockService:StocksService) { }
    
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
