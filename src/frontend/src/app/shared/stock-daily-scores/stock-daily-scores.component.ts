import {Component, Input} from '@angular/core';
import {DailyOutcomeScoresComponent} from "../reports/daily-outcome-scores.component";
import {DatePipe, NgIf} from "@angular/common";
import {ErrorDisplayComponent} from "../error-display/error-display.component";
import {LoadingComponent} from "../loading/loading.component";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {DailyPositionReport, StocksService} from "../../services/stocks.service";
import {GetErrors, parseDate} from "../../services/utils";
import {catchError, tap} from "rxjs/operators";

@Component({
  selector: 'app-stock-daily-scores',
    imports: [
        DailyOutcomeScoresComponent,
        DatePipe,
        ErrorDisplayComponent,
        LoadingComponent,
        NgIf,
        ReactiveFormsModule,
        FormsModule
    ],
  templateUrl: './stock-daily-scores.component.html',
  styleUrl: './stock-daily-scores.component.css'
})
export class StockDailyScoresComponent {

    dailyBreakdowns: DailyPositionReport
    dailyBreakdownsLoading: boolean = true;
    dailyBreakdownErrors: string[] = [];
    selectedStartDate: Date = null;
    selectedEndDate: Date = null;
    
    constructor(private stockService: StocksService) {
        this.selectedStartDate = new Date();
        this.selectedEndDate = new Date();
        this.selectedStartDate.setDate(this.selectedStartDate.getDate() - 365);
    }
    
    _ticker: string;
    @Input()
    get ticker() {
        return this._ticker;
    }
    set ticker(value: string) {
        this._ticker = value;
        this.refreshDailyBreakdowns();
    }
    
    @Input()
    set startDate(value: string) {
        // sometimes the date comes in as a string with a time, we only want the date
        value = value.split('T')[0];
        this.selectedStartDate = parseDate(value);
        this.refreshDailyBreakdowns();
    }

    onStartDateChange($event) {
        if ($event) {
            this.selectedStartDate = parseDate($event);
        } else {
            this.selectedStartDate = null;
        }

        this.refreshDailyBreakdowns();
    }

    onEndDateChange($event) {
        if ($event) {
            this.selectedEndDate = parseDate($event);
        } else {
            this.selectedEndDate = null;
        }

        this.refreshDailyBreakdowns();
    }

    private dailyReportFetch() {
        this.dailyBreakdowns = null;
        this.dailyBreakdownsLoading = true;
        this.dailyBreakdownErrors = [];
        const startStr = this.selectedStartDate.toISOString().split('T')[0];
        const endStr = this.selectedEndDate.toISOString().split('T')[0];
        return this.stockService.reportDailyTickerReport(this.ticker, startStr, endStr)
            .pipe(
                tap(report => {
                    this.dailyBreakdowns = report
                    this.dailyBreakdownsLoading = false;
                }),
                catchError(err => {
                    this.dailyBreakdownErrors = GetErrors(err);
                    this.dailyBreakdownsLoading = false;
                    return [];
                })
            );
    }

    refreshDailyBreakdowns() {
        if (this.selectedStartDate && this.selectedEndDate) {
            const dailyReport = this.dailyReportFetch()
            dailyReport.subscribe();
        }
    }
    
    setDateRange(days: number, event: Event) {
        event.preventDefault();
        this.selectedEndDate = new Date();
        this.selectedStartDate = new Date();
        this.selectedStartDate.setDate(this.selectedStartDate.getDate() - days);
        this.refreshDailyBreakdowns();
    }
}
