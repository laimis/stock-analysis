import {Component, EventEmitter, Input, Output} from '@angular/core';
import {DailyOutcomeScoresComponent} from "../reports/daily-outcome-scores.component";
import {DatePipe, NgIf} from "@angular/common";
import {ErrorDisplayComponent} from "../error-display/error-display.component";
import {LoadingComponent} from "../loading/loading.component";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {DailyPositionReport, StocksService} from "../../services/stocks.service";
import {GetErrors, parseDate} from "../../services/utils";
import {catchError, tap} from "rxjs/operators";

interface DailyScoresInput {
    ticker: string;
    startDate?: string;
}

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
    
    _dailyScoresInput : DailyScoresInput;
    @Input()
    get dailyScoresInput() {
        return this._dailyScoresInput;
    }
    set dailyScoresInput(value: DailyScoresInput) {
        this._dailyScoresInput = value;
        if (value.startDate) {
            const startDate = value.startDate.split('T')[0];
            this.selectedStartDate = parseDate(startDate);
        }
        
        this.refreshDailyBreakdowns();
    }

    @Output()
    dailyBreakdownFetched: EventEmitter<DailyPositionReport> = new EventEmitter<DailyPositionReport>();

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
        return this.stockService.reportDailyTickerReport(this.dailyScoresInput.ticker, startStr, endStr)
            .pipe(
                tap(report => {
                    this.dailyBreakdowns = report
                    this.dailyBreakdownsLoading = false;
                    this.dailyBreakdownFetched.emit(report);
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
