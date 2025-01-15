import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    ChartType,
    DailyPositionReport,
    DataPointContainer,
    PositionInstance,
    StockQuote,
    StocksService,
    TickerCorrelation
} from '../services/stocks.service';
import {convertToLocalTime, GetErrors} from "../services/utils";
import {concat} from "rxjs";
import {tap} from "rxjs/operators";
import {StockPositionsService} from "../services/stockpositions.service";
import {DailyOutcomeScoresComponent} from "../shared/reports/daily-outcome-scores.component";
import {LoadingComponent} from "../shared/loading/loading.component";
import {CorrelationsComponent} from "../shared/reports/correlations.component";
import {FormsModule} from "@angular/forms";
import {CanvasJSAngularChartsModule} from "@canvasjs/angular-charts";
import {ErrorDisplayComponent} from "../shared/error-display/error-display.component";
import {DatePipe, DecimalPipe, NgFor, NgIf} from "@angular/common";
import {OptionPricing, OptionService} from "../services/option.service";
import {LineChartComponent} from "../shared/line-chart/line-chart.component";

function unrealizedProfit(position: PositionInstance, quote: StockQuote) {
    return position.profit + (quote.price - position.averageCostPerShare) * position.numberOfShares
}

function createProfitScatter(entries: PositionInstance[], quotes: Map<string, StockQuote>) {
    const mapped = entries.map(p => {
        return {x: p.daysHeld, y: unrealizedProfit(p, quotes[p.ticker]), label: p.ticker}
    })

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Position Profit / Days Held",
        },
        axisX: {
            title: "Days Held",
            // valueFormatString: "YYYY-MM-DD",
            gridThickness: 0.1,
        },
        axisY: {
            title: "Profit",
            gridThickness: 0.1,
        },
        data: [
            {
                type: "scatter",
                showInLegend: true,
                name: "Position",
                dataPoints: mapped
            }
        ]
    }
}

@Component({
    selector: 'app-playground',
    templateUrl: './playground.component.html',
    styleUrls: ['./playground.component.css'],
    imports: [
        DailyOutcomeScoresComponent,
        LoadingComponent,
        CorrelationsComponent,
        FormsModule,
        CanvasJSAngularChartsModule,
        ErrorDisplayComponent,
        NgIf,
        NgFor,
        LineChartComponent,
        DatePipe,
        DecimalPipe
    ]
})
export class PlaygroundComponent implements OnInit {
    tickers: string[];
    errors: string[];
    status: string;
    testTicker: string;
    Infinity = Infinity
    
    constructor(
        private stocks: StocksService,
        private stockPositions: StockPositionsService,
        private optionService: OptionService,
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        const tickerParam = this.route.snapshot.queryParamMap.get('tickers')
        this.tickers = tickerParam ? tickerParam.split(',') : ['AMD']
        this.testTicker = this.tickers[0]
    }

    correlations: TickerCorrelation[];
    daysForCorrelations: number = 60
    loadingCorrelations: boolean = false
    runCorrelations() {
        this.loadingCorrelations = true
        this.stocks.reportPortfolioCorrelations(this.daysForCorrelations).subscribe((data) => {
            this.correlations = data
            this.loadingCorrelations = false
        })
    }

    chartOptions: any[] = []
    loadingScatterPlot: boolean = false
    runScatterPlot() {
        this.loadingScatterPlot = true
        this.stockPositions.getTradingEntries().subscribe((data) => {
            const tradingPositions = data.current
            const quotes = data.prices

            const profitScatter = createProfitScatter(tradingPositions, quotes)

            this.chartOptions.push(profitScatter)
            this.loadingScatterPlot = false
        }, (error) => {
            this.errors = GetErrors(error)
            this.loadingScatterPlot = false
        })
    }
    
    loadingCombinedDailyChart: boolean = false
    dailyReport: DailyPositionReport
    runCombinedDailyChart() {
        this.loadingCombinedDailyChart = true
        this.stocks.reportDailyPositionReport("6d5e5329-ecc8-41c5-aba1-d94ed914885f").subscribe((data) => {
            this.dailyReport = data
            this.loadingCombinedDailyChart = false
        }, (error) => {
            this.errors = GetErrors(error)
            this.loadingCombinedDailyChart = false
        })
    }

    run() {

        this.status = "Running"

        let positionReport = this.stocks.reportPositions().pipe(
            tap((data) => {
                this.status = "Positions done"
                console.log("Positions")
                console.log(data)
            })
        )

        let singleBarDaily = this.stocks.reportOutcomesSingleBarDaily(this.tickers).pipe(
            tap((data) => {
                this.status = "Single bar daily done"
                console.log("Single bar daily")
                console.log(data)
            }, (error) => {
                console.log("Single bar daily error")
                this.errors = GetErrors(error)
            })
        )

        let multiBarDaily = this.stocks.reportOutcomesAllBars(this.tickers).pipe(
            tap((data) => {
                this.status = "Multi bar daily done"
                console.log("Multi bar daily")
                console.log(data)
            }, (error) => {
                console.log("Multi bar daily error")
                this.errors = GetErrors(error)
            })
        )

        let singleBarWeekly = this.stocks.reportOutcomesSingleBarWeekly(this.tickers).pipe(
            tap((data) => {
                this.status = "Single bar weekly done"
                console.log("Single bar weekly")
                console.log(data)
            })
        )

        let gapReport = this.stocks.reportTickerGaps(this.tickers[0]).pipe(
            tap((data) => {
                this.status = "Gaps done"
                console.log("Gaps")
                console.log(data)
            })
        )

        this.status = "Running..."

        concat([positionReport, gapReport, singleBarDaily, singleBarWeekly, multiBarDaily]).subscribe()
    }

    optionSymbols: string = 'CELH  250221C00027500';
    optionPricingData: OptionPricing[] = [];
    loadingOptionPricing: boolean = false;
    optionChartOptions: any[] = []
    optionDataContainers: DataPointContainer[] = []
    fetchOptionPricing() {
        this.errors = [];
        this.loadingOptionPricing = true;
        const symbols = this.optionSymbols
        this.optionService.getOptionPricing(symbols).subscribe((data) => {
            this.optionPricingData = data;
            this.generateOptionPricingChart();
            this.loadingOptionPricing = false;
        }, (error) => {
            this.errors = GetErrors(error);
            this.loadingOptionPricing = false;
        });
    }

    generateOptionPricingChart() {
        // turn the option pricing data into datapoint container structures that then can be rendered by the line component
        let dateWithoutMilliseconds = (date: Date) => {
            return new Date(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds())
        }
        
        // first, let's get the mark changes over time
        const markChanges = this.optionPricingData.map(op => {
            let date = convertToLocalTime(
                dateWithoutMilliseconds(
                    new Date(op.timestamp)
                )
            );
            let dateStr = date.toISOString();
            return {label: dateStr, value: op.mark, isDate: false}
        });
        
        let container : DataPointContainer = {
            label: "Mark vs Time",
            chartType: ChartType.Line,
            data: markChanges
        }
        
        // underlying price changes
        const underlyingChanges = this.optionPricingData.map(op => {
            let date = convertToLocalTime(
                dateWithoutMilliseconds(
                    new Date(op.timestamp)
                )
            );
            let dateStr = date.toISOString();
            return {label: dateStr, value: op.underlyingPrice, isDate: false}
        });
        
        let underlyingContainer : DataPointContainer = {
            label: "Underlying Price vs Time",
            chartType: ChartType.Line,
            data: underlyingChanges
        }
        
        let deltaChanges = this.optionPricingData.map(op => {
            let date = convertToLocalTime(
                dateWithoutMilliseconds(
                    new Date(op.timestamp)
                )
            );
            let dateStr = date.toISOString();
            return {label: dateStr, value: op.delta, isDate: false}
        });
        let deltaContainer : DataPointContainer = {
            label: "Delta vs Time",
            chartType: ChartType.Line,
            data: deltaChanges
        }
        
        this.optionDataContainers = [container, underlyingContainer, deltaContainer];
    }
}
