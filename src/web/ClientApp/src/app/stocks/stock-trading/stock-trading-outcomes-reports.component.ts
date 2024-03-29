import {Component, Input, OnInit} from '@angular/core';
import {GetErrors} from 'src/app/services/utils';
import {StocksService, OutcomesReport, PositionInstance, StockGaps} from '../../services/stocks.service';
import {tap} from "rxjs/operators";
import {forkJoin} from "rxjs";

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
            this.loading.daily = true
            this.loading.weekly = true
            this.loading.positions = true
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
        let positionReport = this.service.reportPositions().pipe(
            tap(
                report => {
                    this.loading.positions = false
                    this.positionsReport = report
                }, error => {
                    this.loading.positions = false
                    this.handleApiError("Unable to load position reports", error, (e) => this.errors.positions = e)
                }
            )
        )

        this.tickers = positions.map(p => p.ticker)
        let dailyReport = this.service.reportOutcomesSingleBarDaily(this.tickers).pipe(
            tap(
                report => {
                    this.loading.daily = false
                    this.singleBarReportDaily = report
                },
                error => {
                    this.loading.daily = false
                    this.handleApiError("Unable to load daily data", error, (e) => this.errors.daily = e)
                }
            )
        )
        
        let weeklyReport = this.service.reportOutcomesSingleBarWeekly(this.tickers).pipe(
            tap(
                report => {
                    this.loading.weekly = false
                    this.singleBarReportWeekly = report
                }, error => {
                    this.loading.weekly = false
                    this.handleApiError("Unable to load weekly data", error, (e) => this.errors.weekly = e)
                }
            )
        )
        
        forkJoin([positionReport, dailyReport, weeklyReport]).subscribe(
            (_) => {
                console.log("Main Done")
            },
            (error) => {
                console.error("Main Error")
                console.error(error)
                this.errors.positions = GetErrors(error)
            }
        )
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
