import {Component, Input} from '@angular/core';
import {GetErrors} from 'src/app/services/utils';
import {StocksService, OutcomesReport, PositionInstance, StockGaps} from '../../services/stocks.service';
import {catchError, finalize, map} from "rxjs/operators";
import {concat, EMPTY} from "rxjs";

@Component({
    selector: 'app-stock-trading-outcomes-reports',
    templateUrl: './stock-trading-outcomes-reports.component.html',
    styleUrls: ['./stock-trading-outcomes-reports.component.css']
})
export class StockPositionReportsComponent {

    allBarsReport: OutcomesReport;
    singleBarReportDaily: OutcomesReport;
    singleBarReportWeekly: OutcomesReport;
    positionsReport: OutcomesReport;
    gaps: StockGaps[] = [];
    tickerFilter: string;

    loading = {
        daily: false,
        weekly: false,
        allBars: false,
        positions: false
    }

    errors = {
        daily: null,
        weekly: null,
        allBars: null,
        positions: null
    }

    constructor(private service: StocksService) {
    }

    @Input()
    set dailyAnalysis(value: PositionInstance[]) {
        if (value) {
            this.loadPositionData(value)
        }
    }

    @Input()
    set allTimeAnalysis(value: PositionInstance[]) {
        if (value) {
            this.loading.allBars = true
            this.loadAllTimeData(value)
        }
    }

    tickers: string[] = []

    loadPositionData(positions: PositionInstance[]) {
        this.loading.daily = true
        this.loading.weekly = true
        this.loading.positions = true

        const positionReport$ = this.service.reportPositions().pipe(
            map(report => {this.positionsReport = report}),
            catchError(
                error => {
                    this.handleApiError("Unable to load position reports", error, (e) => this.errors.positions = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.positions = false)
        )

        this.tickers = positions.map(p => p.ticker)
        const dailyReport$ = this.service.reportOutcomesSingleBarDaily(this.tickers).pipe(
            map(report => this.singleBarReportDaily = report),
            catchError(
                error => {
                    this.handleApiError("Unable to load daily data", error, (e) => this.errors.daily = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.daily = false)
        )
        
        const weeklyReport$ = this.service.reportOutcomesSingleBarWeekly(this.tickers).pipe(
            map(report => {this.singleBarReportWeekly = report}), 
            catchError(
                error => {
                    this.handleApiError("Unable to load weekly data", error, (e) => this.errors.weekly = e)
                    return EMPTY
                }
            ),
            finalize(() => this.loading.weekly = false)
        )
        
        concat(positionReport$, dailyReport$, weeklyReport$).subscribe()
    }

    private handleApiError(errorMessage: string, error: any, assignFunc: (error: any) => void) {
        const extractedErrors = GetErrors(error);
        extractedErrors.forEach(e => console.log(e))
        const fullError = errorMessage + ": " + extractedErrors.join(", ")
        assignFunc([fullError])
    }

    loadAllTimeData(positions: PositionInstance[]) {
        this.tickers = positions.map(p => p.ticker)
        this.service.reportOutcomesAllBars(this.tickers).subscribe(report => {
            this.loading.allBars = false
            this.allBarsReport = report
            this.gaps = report.gaps
        }, error => {
            this.loading.allBars = false
            this.handleApiError("Unable to load all bars", error, (e) => this.errors.allBars = e)
        })
    }

    onTickerChange(ticker: string) {
        this.tickerFilter = ticker
    }
}
