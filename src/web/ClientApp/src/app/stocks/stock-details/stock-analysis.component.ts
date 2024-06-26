import {CurrencyPipe, DecimalPipe, PercentPipe} from '@angular/common';
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

@Component({
    selector: 'app-stock-analysis',
    templateUrl: './stock-analysis.component.html',
    styleUrls: ['./stock-analysis.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class StockAnalysisComponent {
    multipleBarOutcomes: TickerOutcomes;

    dailyOutcomesReport: OutcomesReport;
    dailyOutcomes: TickerOutcomes;

    dailyBreakdowns: DailyPositionReport
    
    gaps: StockGaps;
    percentChangeDistribution: StockPercentChangeResponse;
    chartInfo: PositionChartInformation
    @Input()
    ticker: string

    constructor(
        private stockService: StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
    }
    
    @Input()
    startDate: string
    
    @Input()
    endDate: string

    @Input()
    set prices(prices: Prices) {
        this.chartInfo = {
            ticker: this.ticker,
            prices: prices,
            transactions: [],
            markers: [],
            averageBuyPrice: null,
            stopPrice: null
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
