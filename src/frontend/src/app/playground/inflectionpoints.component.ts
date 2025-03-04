import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    ChartType,
    DataPoint,
    DataPointContainer,
    PriceBar,
    PriceFrequency,
    Prices,
    StocksService
} from '../services/stocks.service';
import {GetErrors, humanFriendlyDuration} from "../services/utils";
import {
    age,
    calculateInflectionPoints,
    Gradient,
    InfectionPointType,
    InflectionPoint,
    InflectionPointLog,
    toHistogram,
    toInflectionPointLog,
    toValley,
    TrendAnalysisResult,
    TrendChangeAlert
} from "../services/inflectionpoints.service";

import { PeakValleyAnalysisComponent } from '../shared/peak-valley-analysis/peak-valley-analysis.component';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LineChartComponent } from "../shared/line-chart/line-chart.component";

function nextDay(currentDate: string): string {
    let d = new Date(currentDate + " 23:00:00") // adding hours to make sure that when input is treated as midnight UTC, it is still the same day as the input
    let date = new Date(d.getFullYear(), d.getMonth(), d.getDate())
    date.setDate(date.getDate() + 1)
    return date.toISOString().substring(0, 10)
}

function histogramToDataPointContainer(title: string, histogram: {}) {
    let dataPoints: DataPoint[] = []
    for (let key in histogram) {
        dataPoints.push({
            label: key,
            isDate: false,
            value: histogram[key]
        })
    }
    return {
        label: title,
        chartType: ChartType.Column,
        data: dataPoints
    }
}

function logToDataPointContainer(label: string, log: InflectionPointLog[]) {
    let dataPoints: DataPoint[] = []
    for (let i = 0; i < log.length; i++) {
        let point = log[i]
        dataPoints.push({
            label: point.from.gradient.dataPoint.dateStr,
            isDate: true,
            value: Math.round(point.percentChange * 100)
        })
    }
    return {
        label: label,
        chartType: ChartType.Column,
        data: dataPoints
    }
}

function toDailyBreakdownDataPointCointainer(label: string, points: InflectionPoint[], priceFunc?): DataPointContainer {

    if (priceFunc) {
        points = points.map(p => {
            let bar: PriceBar = {
                dateStr: p.gradient.dataPoint.dateStr,
                close: priceFunc(p.gradient.dataPoint.close),
                open: priceFunc(p.gradient.dataPoint.open),
                high: priceFunc(p.gradient.dataPoint.high),
                low: priceFunc(p.gradient.dataPoint.low),
                volume: p.gradient.dataPoint.volume
            }
            let newGradient: Gradient = {
                dataPoint: bar,
                delta: p.gradient.delta,
                index: p.gradient.index
            }
            return toValley(newGradient)
        })
    }

    // we want to generate a data point for each day, even if there is no peak or valley
    // start with the first date and keep on going until we reach the last date
    let currentDate = points[0].gradient.dataPoint.dateStr
    let currentIndex = 0
    let dataPoints: DataPoint[] = []

    while (currentIndex < points.length) {

        if (currentDate == points[currentIndex].gradient.dataPoint.dateStr) {

            dataPoints.push({
                label: points[currentIndex].gradient.dataPoint.dateStr,
                isDate: true,
                value: points[currentIndex].priceValue
            })

            currentIndex++

        } else {

            dataPoints.push({
                label: currentDate,
                isDate: true,
                value: points[currentIndex - 1].priceValue
            })

        }
        currentDate = nextDay(currentDate)
    }

    return {
        label: label,
        chartType: ChartType.Scatter,
        data: dataPoints
    }
}

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
    obvContainer: DataPointContainer;

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

        let startDate = new Date();
        startDate.setDate(startDate.getDate() - 365);

        this.stocks.reportDailyTickerReport(tickers[0], startDate.toISOString().split('T')[0]).subscribe(
            result => {
                this.obvContainer = result.dailyObv
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
