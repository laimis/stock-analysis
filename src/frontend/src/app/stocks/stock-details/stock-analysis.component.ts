import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, NgIf, PercentPipe} from '@angular/common';
import {Component, Input} from '@angular/core';
import {
    DailyPositionReport,
    DataPointContainer,
    OutcomesReport,
    OutcomeValueTypeEnum,
    PositionChartInformation,
    PriceBar,
    Prices,
    StockAnalysisOutcome,
    StockGaps,
    StockPercentChangeResponse,
    StocksService,
    TickerOutcomes,
    TrendAnalysisResult,
    TrendChangeAlert
} from 'src/app/services/stocks.service';
import {catchError, tap} from "rxjs/operators";
import {concat} from "rxjs";
import {GapsComponent} from "../../shared/reports/gaps.component";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";
import {OutcomesAnalysisReportComponent} from "../../shared/reports/outcomes-analysis-report.component";
import {PercentChangeDistributionComponent} from "../../shared/reports/percent-change-distribution.component";
import {PriceChartComponent} from "../../shared/price-chart/price-chart.component";
import {FormsModule} from "@angular/forms";
import {StockDailyScoresComponent} from "../../shared/stock-daily-scores/stock-daily-scores.component";
import { PeakValleyAnalysisComponent } from "../../shared/peak-valley-analysis/peak-valley-analysis.component";
import { InflectionPoint } from 'src/app/services/inflectionpoints.service';

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
    PriceChartComponent,
    FormsModule,
    StockDailyScoresComponent,
    PeakValleyAnalysisComponent
]
})
export class StockAnalysisComponent {
    multipleBarOutcomes: TickerOutcomes;

    dailyOutcomesReport: OutcomesReport;
    dailyOutcomes: TickerOutcomes;

    gaps: StockGaps;
    percentChangeDistribution: StockPercentChangeResponse;
    chartInfo: PositionChartInformation
    obvContainer: DataPointContainer;
    private _prices: Prices;
    pricesTrendDataSet: { prices: PriceBar[]; inflectionPoints: InflectionPoint[]; trendAnalysis: { establishedTrend: TrendAnalysisResult; potentialChange: TrendChangeAlert; }; };
    obvTrendDataSet: { prices: PriceBar[]; inflectionPoints: InflectionPoint[]; trendAnalysis: { establishedTrend: TrendAnalysisResult; potentialChange: TrendChangeAlert; }; };
    
    constructor(
        private stockService: StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
    }
    
    @Input()
    ticker: string
    @Input()
    startDate: string
    @Input()
    endDate: string

    @Input()
    set prices(prices: Prices) {
        this._prices = prices;
        this.chartInfo = {
            ticker: this.ticker,
            prices: prices.prices,
            transactions: [],
            markers: [],
            averageBuyPrice: null,
            stopPrice: null,
            buyOrders: [],
            sellOrders: [],
            movingAverages: prices.movingAverages
        }
        this.loadData(this.ticker)
    }
    get prices(): Prices {
        return this._prices
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

        const inflectionReport = this.stockService.reportInflectionPoints(ticker, this.startDate)
            .pipe(
                tap(data => {
                    this.pricesTrendDataSet = {
                        prices: data.prices,
                        inflectionPoints: data.priceInflectionPoints,
                        trendAnalysis: data.priceTrendAnalysis
                    }
                    this.obvTrendDataSet = {
                        prices: data.obv,
                        inflectionPoints: data.obvInflectionPoints,
                        trendAnalysis: data.obvTrendAnalysis
                    }
                }),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            )
        
        concat(inflectionReport, multipleBarOutcomesPromise, dailyOutcomesPromise, percentDistribution).subscribe();
    }

    dailyBreakdownFetched($event: DailyPositionReport) {
        this.obvContainer = $event.dailyObv;
    }
}
