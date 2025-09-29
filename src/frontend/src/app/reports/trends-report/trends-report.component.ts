import {Component, OnInit} from '@angular/core';
import {
    ChartType,
    DataPoint,
    DataPointContainer,
    StocksService,
    Trend,
    TrendDirection,
    Trends,
    TrendType,
    ValueWithFrequency
} from "../../services/stocks.service";
import { NgClass, PercentPipe } from "@angular/common";
import {FormsModule} from "@angular/forms";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {StockSearchComponent} from "../../stocks/stock-search/stock-search.component";
import {LineChartComponent} from "../../shared/line-chart/line-chart.component";
import {GetErrors} from "../../services/utils";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {ActivatedRoute} from "@angular/router";
import {catchError, tap} from "rxjs/operators";
import {concat, EMPTY} from "rxjs";
import {TradingViewLinkComponent} from "../../shared/stocks/trading-view-link.component";

@Component({
    selector: 'app-trends-report',
    imports: [
    PercentPipe,
    NgClass,
    FormsModule,
    LoadingComponent,
    StockSearchComponent,
    LineChartComponent,
    ErrorDisplayComponent,
    TradingViewLinkComponent
],
    templateUrl: './trends-report.component.html',
    styleUrl: './trends-report.component.css'
})
export class TrendsReportComponent implements OnInit {
    trends: Trends;
    currentTrend: Trend;
    loading: boolean = false

    selectedTicker: string;
    selectedStartDate: string;
    selectedTrendType: TrendType;
    
    Ema20OverSma50 = TrendType.Ema20OverSma50
    Sma50OverSma200 = TrendType.Sma50OverSma200

    dataPointContainers: DataPointContainer[] = []
    errors: string[];
    protected readonly TrendDirection = TrendDirection;
    

    constructor(private stockService: StocksService, private route: ActivatedRoute) {
        // Set default values
        this.selectedStartDate = "10";
        this.selectedTrendType = TrendType.Ema20OverSma50
    }

    loadTrends() {
        
        if (!this.selectedTicker) {
            return
        }

        this.loading = true
        const date = new Date();
        date.setFullYear(date.getFullYear() - parseInt(this.selectedStartDate));
        const selectedStartDate = date.toISOString().substring(0, 10);

        this.stockService.reportTrends(this.selectedTicker, this.selectedTrendType, selectedStartDate).subscribe(data => {
            this.trends = data
            this.currentTrend = data.currentTrend

            // chronological
            let barsChronological: DataPoint[] = data.trends.map((t: Trend) => {
                return {
                    label: t.startDateStr,
                    isDate: true,
                    value: t.direction === TrendDirection.Up ? t.numberOfBars : -t.numberOfBars
                }
            })
            let gainsChronological: DataPoint[] = data.trends.map((t: Trend) => {
                return {label: t.startDateStr, isDate: true, value: t.gainPercent}
            })

            this.dataPointContainers = [

                {label: "Gains Chronological", chartType: ChartType.Column, data: gainsChronological},
                {label: "Bars Chronological", chartType: ChartType.Column, data: barsChronological},

                this.sortAndCreateContainer(data.upTrends.map((t: Trend) => t.numberOfBars), "Up Bars Sorted"),
                this.sortAndCreateContainer(data.upTrends.map((t: Trend) => t.gainPercent), "Up Gain Sorted"),
                
                this.sortAndCreateContainer(data.downTrends.map((t: Trend) => t.numberOfBars), "Down Bars Sorted"),
                this.sortAndCreateContainer(data.downTrends.map((t: Trend) => t.gainPercent), "Down Gain Sorted"),
                
                this.createContainer(data.upBarStatistics.buckets, "Up Bar Distribution"),
                this.createContainer(data.upGainStatistics.buckets, "Up Gain Distribution"),
                
                this.createContainer(data.downBarStatistics.buckets, "Down Bar Distribution"),
                this.createContainer(data.downGainStatistics.buckets, "Down Gain Distribution"),

            ]

            this.loading = false
        }, error => {
            console.log("Error: " + error)
            this.errors = GetErrors(error)
            this.loading = false
        })
    }

    tickerSelected(ticker: string, trendType: TrendType) {
        this.selectedTicker = ticker
        this.selectedTrendType = trendType
        this.applyFilters()
    }

    trendTypeSelected(newTrendType: TrendType) {
        this.selectedTrendType = newTrendType
        this.applyFilters()
    }

    startDateSelected(newDate: string) {
        this.selectedStartDate = newDate
        this.applyFilters()
    }

    applyFilters() {
        this.currentTrend = null
        this.trends = null
        this.dataPointContainers = null
        this.loadTrends()
    }

    sortAndCreateContainer(numbers: number[], title: string) {
        let sorted: DataPoint[] = numbers.sort((a, b) => a - b).map((value: number, index: number) => {
            return {label: (index + 1).toString(), isDate: false, value: value}
        })
        return {
            label: title,
            chartType: ChartType.Column,
            data: sorted
        }
    }

    createContainer(data: ValueWithFrequency[], title: string) {
        let dataPoints: DataPoint[] = []
        data.forEach((b: ValueWithFrequency) => {
            dataPoints.push({
                label: b.value.toString(),
                isDate: false,
                value: b.frequency
            })
        })

        return {
            label: title,
            chartType: ChartType.Column,
            data: dataPoints
        }
    }

    ngOnInit(): void {
        this.route.queryParams.subscribe(params => {
            if (params['tickers']) {
                let tickers = params['tickers'].split(',');
                this.loadSummary(tickers)
            }
        }, error => {
            this.errors = GetErrors(error);
        });
    }
    
    shortTermTrendSummary = new Map<string, Trends>()
    longTermTrendSummary = new Map<string, Trends>()
    
    loadSummary(tickers: string[]) {
        
        // for each ticker in the list, setup an observable that will make reportTrends call
        // when the observable is subscribed to
        let shortTermObservables = tickers.map(ticker => {
            return this.stockService.reportTrends(ticker, TrendType.Ema20OverSma50, "10")
                .pipe(
                    tap(data => this.shortTermTrendSummary[ticker] = data),
                    catchError(error => {
                        console.log("Error: " + error)
                        this.errors = GetErrors(error)
                        return EMPTY
                    })
                )
        })
        
        let longTermObservables = tickers.map(ticker => {
            return this.stockService.reportTrends(ticker, TrendType.Sma50OverSma200, "10")
                .pipe(
                    tap(data => this.longTermTrendSummary[ticker] = data),
                    catchError(error => {
                        console.log("Error: " + error)
                        this.errors = GetErrors(error)
                        return EMPTY
                    })
                )
        })
        
        let allObservables = shortTermObservables.concat(longTermObservables)
        
        concat(...allObservables).subscribe()
    }
    
    loadSummaryIncremental(tickers: string[], trendSummaryContainer: {}) {
        // if there are no tickers, do nothing
        if (tickers.length == 0) {
            return
        }
        
        // take the first ticker
        let ticker = tickers[0]

        this.stockService.reportTrends(ticker, TrendType.Ema20OverSma50, "10")
            .subscribe(
                data => {
                    trendSummaryContainer[ticker] = data
                    // call the function recursively with the rest of the tickers
                    this.loadSummaryIncremental(tickers.slice(1), trendSummaryContainer)
                },
                error => {
                    console.log("Error: " + error)
                    this.errors = GetErrors(error)
                    this.loadSummaryIncremental(tickers.slice(1), trendSummaryContainer)
                }
            )
    }

    protected readonly Object = Object;
}
