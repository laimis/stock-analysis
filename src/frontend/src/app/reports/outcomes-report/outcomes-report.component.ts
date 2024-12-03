import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {GetErrors, toggleVisuallyHidden} from 'src/app/services/utils';
import {OutcomesReport, StocksService, TickerCorrelation, TickerOutcomes} from '../../services/stocks.service';
import {catchError, map, tap} from "rxjs/operators";
import {concat} from "rxjs";

@Component({
    selector: 'app-outcomes-report',
    templateUrl: './outcomes-report.component.html',
    styleUrls: ['./outcomes-report.component.css'],
    standalone: false
})
export class OutcomesReportComponent implements OnInit {
    errors: string[] = null;
    startDate: string = null;
    endDate: string = null;
    earnings: string[] = [];
    title: string;
    tickers: string[] = [];
    excludedTickers: string[] = [];

    activeTicker: string = null;

    allBarsReport: OutcomesReport;
    singleBarReportDaily: OutcomesReport;
    singleBarReportWeekly: OutcomesReport;
    earningsOutcomes: TickerOutcomes[];
    correlationsReport: TickerCorrelation[]
    correlationsDays: number = 60;
    excludeEarningsTitle: string = "Exclude Earnings";

    constructor(
        private stocksService: StocksService,
        private route: ActivatedRoute) {
    }

    ngOnInit(): void {
        var titleParam = this.route.snapshot.queryParamMap.get("title");
        if (titleParam) {
            this.title = titleParam;
        }

        var earningsParam = this.route.snapshot.queryParamMap.get("earnings");
        if (earningsParam) {
            this.earnings = earningsParam.split(",")
        }

        var startDateParam = this.route.snapshot.queryParamMap.get("startDate");
        if (startDateParam) {
            this.startDate = startDateParam;
        }

        var endDateParam = this.route.snapshot.queryParamMap.get("endDate");
        if (endDateParam) {
            this.endDate = endDateParam;
        }

        const tickerParam = this.route.snapshot.queryParamMap.get("tickers");
        if (tickerParam) {
            this.tickers = tickerParam
                .split(",")
                .map(t => {
                    const parts = t.split(":");
                    if (parts.length == 2) {
                        return parts[1]
                    }
                    return t;
                });

            if (this.tickers.length > 0) {
                this.loadData()
            }

        } else {
            this.errors = ["No tickers were provided"];
        }
    }

    runReports(start, end) {
        if (this.tickers.length === 0) {
            return
        }

        this.startDate = start
        this.endDate = end
        this.loadData()
    }

    loadData() {
        if (this.tickers.length === 0) {
            return
        }

        this.reset()

        const singleBarDailyReport = this.stocksService.reportOutcomesSingleBarDaily(this.tickers, "Earnings", this.earnings, this.endDate)
            .pipe(
                map(
                    report => {
                        this.singleBarReportDaily = report;
                        if (this.earnings.length > 0) {
                            this.earningsOutcomes = report.outcomes.filter(o => this.earnings.indexOf(o.ticker) >= 0);
                        }
                    }
                ),
                catchError(errors => this.errors = GetErrors(errors))
            )

        const singleBarWeekly = this.stocksService.reportOutcomesSingleBarWeekly(this.tickers, this.endDate)
            .pipe(
                map(report => this.singleBarReportWeekly = report),
                catchError(errors => this.errors = GetErrors(errors))
            )

        const allBarsReport = this.stocksService.reportOutcomesAllBars(this.tickers, this.startDate, this.endDate)
            .pipe(
                map(report => this.allBarsReport = report),
                catchError(errors => this.errors = GetErrors(errors))
            )
        
        const correlationsReport = this.stocksService.reportCorrelations(this.tickers, this.correlationsDays)
            .pipe(
                map(report => this.correlationsReport = report),
                catchError(errors => this.errors = GetErrors(errors))
            )

        concat(singleBarDailyReport, singleBarWeekly, allBarsReport, correlationsReport).subscribe()
    }

    onTickerChange(activeTicker: string) {
        this.activeTicker = activeTicker;
    }

    toggleExcludeEarnings() {
        if (this.excludedTickers.length > 0) {
            this.excludedTickers = [];
            this.excludeEarningsTitle = "Exclude Earnings";
        } else {
            this.excludedTickers = this.earnings;
            this.excludeEarningsTitle = "Include Earnings";
        }
    }

    toggleVisibility(form: HTMLElement, button: HTMLElement) {
        toggleVisuallyHidden(form);
        toggleVisuallyHidden(button);
    }

    private reset() {
        this.errors = null;
        this.allBarsReport = null;
        this.singleBarReportDaily = null;
        this.singleBarReportWeekly = null;
        this.earningsOutcomes = [];
        this.correlationsReport = null;
    }
}
