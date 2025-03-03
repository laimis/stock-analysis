import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    DataPointContainer,
    PositionChartInformation,
    PriceFrequency,
    Prices,
    StocksService
} from '../services/stocks.service';
import {GetErrors, humanFriendlyDuration} from "../services/utils";
import {
    age,
    calculateInflectionPoints,
    getCompleteTrendAnalysis,
    histogramToDataPointContainer,
    InfectionPointType,
    InflectionPointLog,
    logToDataPointContainer,
    toChartMarker,
    toDailyBreakdownDataPointCointainer,
    toHistogram,
    toInflectionPointLog,
    TrendAnalysisResult,
    TrendChangeAlert
} from "../services/prices.service";

import { PeakValleyAnalysisComponent } from '../shared/peak-valley-analysis/peak-valley-analysis.component';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LineChartComponent } from "../shared/line-chart/line-chart.component";

@Component({
    selector: 'app-inflection-points',
    templateUrl: './inflectionpoints.component.html',
    imports: [
    PeakValleyAnalysisComponent,
    ErrorDisplayComponent,
    FormsModule,
    PercentPipe,
    LineChartComponent,
    CurrencyPipe
]
})
export class InflectionPointsComponent implements OnInit {
    tickers: string[];
    options: any;
    prices: Prices;
    priceFrequency: PriceFrequency = PriceFrequency.Daily;
    ageValueToUse: number;
    lineContainers: DataPointContainer[];
    peaksAndValleys: DataPointContainer[];
    log: InflectionPointLog[];
    errors: string[];
    trendAnalysisResult: TrendAnalysisResult;
    trendChangeAlert: TrendChangeAlert;
    protected readonly twoMonths = 365 / 6;
    protected readonly sixMonths = 365 / 2;

    constructor(
        private stocks: StocksService,
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        this.ageValueToUse = this.twoMonths
        const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
        this.tickers = tickerParam ? tickerParam.split(',') : ['AMD'];
        this.renderPrices(this.tickers)
    }

    renderPrices(tickers: string[]) {
        this.stocks.getStockPrices(tickers[0], 365, this.priceFrequency).subscribe(
            result => {
                this.prices = result
                const inflectionPoints = calculateInflectionPoints(result.prices);
                const peaks = inflectionPoints.filter(p => p.type === InfectionPointType.Peak)
                const valleys = inflectionPoints.filter(p => p.type === InfectionPointType.Valley)

                
                const peaksContainer = toDailyBreakdownDataPointCointainer('peaks', peaks)
                const valleysContainer = toDailyBreakdownDataPointCointainer('valleys', valleys)
                const smoothedPeaks = toDailyBreakdownDataPointCointainer('smoothed peaks', peaks, (p: number) => Math.round(p))
                const smoothedValleys = toDailyBreakdownDataPointCointainer('smoothed valleys', valleys, (p: number) => Math.round(p))

                this.log = toInflectionPointLog(inflectionPoints).reverse()

                const humanFriendlyTimeDuration = humanFriendlyDuration(this.ageValueToUse)
                const resistanceHistogram = toHistogram(peaks.filter(p => age(p) < this.ageValueToUse))
                const resistanceHistogramPointContainer = histogramToDataPointContainer(humanFriendlyTimeDuration + ' resistance histogram', resistanceHistogram)

                const supportHistogram = toHistogram(valleys.filter(p => age(p) < this.ageValueToUse))
                const supportHistogramPointContainer = histogramToDataPointContainer(humanFriendlyTimeDuration + ' support histogram', supportHistogram)

                const logChart = logToDataPointContainer("Log", this.log)

                this.lineContainers = [
                    resistanceHistogramPointContainer, supportHistogramPointContainer, peaksContainer, smoothedPeaks, valleysContainer, smoothedValleys, logChart
                ]

                this.peaksAndValleys = [smoothedPeaks, smoothedValleys]
            },
            error => this.errors = GetErrors(error)
        );
    }

    priceFrequencyChanged() {
        this.renderPrices(this.tickers);
    }

    ageValueChanged() {
        this.renderPrices(this.tickers);
    }
}
