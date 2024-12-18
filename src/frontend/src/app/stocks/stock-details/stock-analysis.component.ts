import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, NgIf, PercentPipe} from '@angular/common';
import {Component, Input} from '@angular/core';
import {
    DailyPositionReport, OutcomesReport,
    OutcomeValueTypeEnum,
    PositionChartInformation,
    Prices,
    StockAnalysisOutcome,
    StockGaps,
    StockPercentChangeResponse,
    StocksService,
    TickerOutcomes
} from 'src/app/services/stocks.service';
import {catchError, tap} from "rxjs/operators";
import {concat} from "rxjs";
import {GapsComponent} from "../../shared/reports/gaps.component";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";
import {OutcomesAnalysisReportComponent} from "../../shared/reports/outcomes-analysis-report.component";
import {PercentChangeDistributionComponent} from "../../shared/reports/percent-change-distribution.component";
import {DailyOutcomeScoresComponent} from "../../shared/reports/daily-outcome-scores.component";
import {CandlestickChartComponent} from "../../shared/candlestick-chart/candlestick-chart.component";
import {FormsModule} from "@angular/forms";

@Component({
    selector: 'app-stock-analysis',
    templateUrl: './stock-analysis.component.html',
    styleUrls: ['./stock-analysis.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe],
    imports: [
        NgIf,
        NgClass,
        DatePipe,
        GapsComponent,
        LineChartComponent,
        OutcomesAnalysisReportComponent,
        PercentChangeDistributionComponent,
        DailyOutcomeScoresComponent,
        CandlestickChartComponent,
        FormsModule
    ]
})
export class StockAnalysisComponent {
    multipleBarOutcomes: TickerOutcomes;

    dailyOutcomesReport: OutcomesReport;
    dailyOutcomes: TickerOutcomes;

    dailyBreakdowns: DailyPositionReport
    
    gaps: StockGaps;
    percentChangeDistribution: StockPercentChangeResponse;
    chartInfo: PositionChartInformation
    
    selectedStartDate: Date = null;
    selectedEndDate: Date = null;
    
    constructor(
        private stockService: StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
        this.selectedStartDate = new Date();
        this.selectedEndDate = new Date();
        this.selectedStartDate.setDate(this.selectedStartDate.getDate() - 30);
    }
    
    @Input()
    startDate: string
    
    @Input()
    endDate: string

    @Input()
    ticker: string

    @Input()
    set prices(prices: Prices) {
        this.chartInfo = {
            ticker: this.ticker,
            prices: prices,
            transactions: [],
            markers: [],
            averageBuyPrice: null,
            stopPrice: null,
            buyOrders: [],
            sellOrders: []
        }
        this.loadData(this.ticker)
    }

    getValue(o: StockAnalysisOutcome) {
        if (o.valueType === OutcomeValueTypeEnum.Percentage) {
            return this.percentPipe.transform(o.value)
        } else if (o.valueType === OutcomeValueTypeEnum.Currency) {
            return this.currencyPipe.transform(o.value)
        } else {
            return this.decimalPipe.transform(o.value)
        }
    }

    positiveCount(outcomes: TickerOutcomes) {
        return outcomes.outcomes.filter(r => r.outcomeType === 'Positive').length;
    }

    negativeCount(outcomes: TickerOutcomes) {
        return outcomes.outcomes.filter(r => r.outcomeType === 'Negative').length;
    }

    onStartDateChange($event) {
        if ($event) {
            this.selectedStartDate = new Date($event);
        } else {
            this.selectedStartDate = null;
        }
        
        this.refreshDailyBreakdowns();
    }
    
    onEndDateChange($event) {
        if ($event) {
            this.selectedEndDate = new Date($event);
        } else {
            this.selectedEndDate = null;
        }
        
        this.refreshDailyBreakdowns();
    }
    
    refreshDailyBreakdowns() {
        if (this.selectedStartDate && this.selectedEndDate) {
            this.dailyBreakdowns = null;
            const startStr = this.selectedStartDate.toISOString().split('T')[0];
            const endStr = this.selectedEndDate.toISOString().split('T')[0];
            const dailyReport = this.stockService.reportDailyTickerReport(this.ticker, startStr, endStr)
                .pipe(
                    tap(report => {this.dailyBreakdowns = report}),
                    catchError(err => {
                        console.error(err);
                        return [];
                    })
                );
            dailyReport.subscribe();
        }
    }
    
    private loadData(ticker:string) {
        const dailyReport = this.stockService.reportDailyTickerReport(ticker)
            .pipe(
                tap(report => {this.dailyBreakdowns = report}),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            );
        
        const multipleBarOutcomesPromise = this.stockService.reportOutcomesAllBars([ticker])
            .pipe(
                tap(report => {
                    this.multipleBarOutcomes = report.outcomes[0];
                    this.gaps = report.gaps[0]
                    }),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            )
        
        const dailyOutcomesPromise = this.stockService.reportOutcomesSingleBarDaily([ticker])
            .pipe(
                tap(report => {
                    this.dailyOutcomes = report.outcomes[0];
                    this.dailyOutcomesReport = report;
                    }),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            )
        
        const percentDistribution = this.stockService.reportTickerPercentChangeDistribution(ticker)
            .pipe(
                tap(data => {
                    this.percentChangeDistribution = data;
                    }),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            )
        
        concat(dailyReport, multipleBarOutcomesPromise, dailyOutcomesPromise, percentDistribution).subscribe();
    }
}
