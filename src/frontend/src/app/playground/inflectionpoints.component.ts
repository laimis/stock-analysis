import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    DataPointContainer,
    Gradient,
    InfectionPointType,
    InflectionPoint,
    InflectionPointsReportDetails,
    StocksService,
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";

import { PeakValleyAnalysisComponent } from '../shared/peak-valley-analysis/peak-valley-analysis.component';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { CurrencyPipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LineChartComponent } from "../shared/line-chart/line-chart.component";

type InflectionPointLog = {
    from: InflectionPoint,
    to: InflectionPoint,
    days: number,
    change: number,
    percentChange: number
}

export function toValley(gradient: Gradient): InflectionPoint {
    return {gradient: gradient, type: InfectionPointType.Valley, priceValue: gradient.dataPoint.close}
}

export function age(point: InflectionPoint): number {
    const date = new Date(point.gradient.dataPoint.dateStr).getTime()
    const now = new Date().getTime()
    return (now - date) / (1000 * 3600 * 24)
}

@Component({
    selector: 'app-inflection-points',
    templateUrl: './inflectionpoints.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
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
    options: object;
    
    
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
        const startDate = new Date();
        startDate.setDate(startDate.getDate() - 365);
        const startDateStr = startDate.toISOString().split('T')[0];

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
