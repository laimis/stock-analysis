import { Component, OnInit, inject } from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    ChartType,
    DataPoint,
    DataPointContainer,
    Gradient,
    InfectionPointType,
    InflectionPoint,
    InflectionPointsReportDetails,
    PriceBar,
    StocksService,
    TrendAnalysisResult
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";

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

type InflectionPointLog = {
    from: InflectionPoint,
    to: InflectionPoint,
    days: number,
    change: number,
    percentChange: number
}

function toPeak(gradient: Gradient): InflectionPoint {
    return {gradient: gradient, type: InfectionPointType.Peak, priceValue: gradient.dataPoint.close}
}

export function toValley(gradient: Gradient): InflectionPoint {
    return {gradient: gradient, type: InfectionPointType.Valley, priceValue: gradient.dataPoint.close}
}

function toInflectionPointLog(inflectionPoints: InflectionPoint[]): InflectionPointLog[] {

    let log: InflectionPointLog[] = []
    let i = 1
    while (i < inflectionPoints.length) {
        let point1 = inflectionPoints[i - 1]
        let point2 = inflectionPoints[i]
        let days = (new Date(point2.gradient.dataPoint.dateStr).getTime() - new Date(point1.gradient.dataPoint.dateStr).getTime()) / (1000 * 3600 * 24)
        let change = point2.gradient.dataPoint.close - point1.gradient.dataPoint.close
        let percentChange = change / point2.gradient.dataPoint.close
        log.push({from: point1, to: point2, days: days, change: change, percentChange: percentChange})
        i += 1
    }
    return log
}

function toHistogram(inflectionPoints: InflectionPoint[]) {
    // build a histogram of the inflection points
    // the histogram will have a key for each price
    // and the value will be the number of times that price was hit
    let histogram = {}

    for (let i = 0; i < inflectionPoints.length; i++) {
        let price = inflectionPoints[i].priceValue

        // we will round the price to the nearest dollar
        let rounded = Math.round(price)

        if (histogram[rounded]) {
            histogram[rounded] += 1
        } else {
            histogram[rounded] = 1
        }
    }

    return histogram
}

export function age(point: InflectionPoint): number {
    const date = new Date(point.gradient.dataPoint.dateStr).getTime()
    const now = new Date().getTime()
    return (now - date) / (1000 * 3600 * 24)
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
    private stocks = inject(StocksService);
    private route = inject(ActivatedRoute);

    tickers: string[];
    options: any;
    
    
    ageValueToUse: number;
    lineContainers: DataPointContainer[];
    peaksAndValleys: DataPointContainer[];
    log: InflectionPointLog[];
    errors: string[];
    protected readonly twoMonths = 365 / 6;
    protected readonly sixMonths = 365 / 2;
    
    completePriceDataSet: InflectionPointsReportDetails

    completeOBVDataSet: InflectionPointsReportDetails


    ngOnInit() {
        this.ageValueToUse = this.twoMonths
        const tickerParam = this.route.snapshot.queryParamMap.get('tickers');
        this.tickers = tickerParam ? tickerParam.split(',') : ['AMD'];
        this.renderPrices(this.tickers)
    }

    renderPrices(tickers: string[]) {
        let startDate = new Date();
        startDate.setDate(startDate.getDate() - 365);
        let startDateStr = startDate.toISOString().split('T')[0];

        this.stocks.reportInflectionPoints(tickers[0], startDateStr).subscribe(
            result => {
                this.completePriceDataSet = result.price
                this.completeOBVDataSet = result.onBalanceVolume
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
