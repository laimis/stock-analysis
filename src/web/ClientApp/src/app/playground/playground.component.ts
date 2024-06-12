import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {
    DailyPositionReport, DataPointContainer,
    PositionInstance,
    StockQuote,
    StocksService,
    TickerCorrelation
} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {concat} from "rxjs";
import {tap} from "rxjs/operators";
import {StockPositionsService} from "../services/stockpositions.service";
import {CanvasJSChart} from "@canvasjs/angular-charts";


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

function createData(container: DataPointContainer, useY2: boolean) {
    return {
        type: "line",
        dataPoints: container.data.map(d => {
            return {x: new Date(Date.parse(d.label)), y: d.value}
        }),
        showInLegend: true,
        name: container.label,
        axisYType: useY2 ? "secondary" : "primary"
    }
}

function createCombinedDailyChart(report:DailyPositionReport) {
    console.log(report)
    const data = [
        createData(report.dailyClose, false),
        createData(report.dailyObv, true)
    ]

    return {
        exportEnabled: true,
        zoomEnabled: true,
        title: {
            text: "Daily Close / OBV",
        },
        axisX: {
            title: "Date",
            valueFormatString: "YYYY-MM-DD",
            gridThickness: 0.1,
        },
        axisY: {title: "Close", gridThickness: 0.1},
        axisY2: {title: "OBV", gridThickness: 0.1},
        data: data
    }
}

@Component({
    selector: 'app-playground',
    templateUrl: './playground.component.html',
    styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {
    tickers: string[];
    errors: string[];
    status: string;
    testTicker: string;
    
    constructor(
        private stocks: StocksService,
        private stockPositions: StockPositionsService,
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
    combinedDailyChartOptions: any
    runCombinedDailyChart() {
        this.loadingCombinedDailyChart = true
        this.stocks.reportDailyPositionReport("6d5e5329-ecc8-41c5-aba1-d94ed914885f").subscribe((data) => {
            this.combinedDailyChartOptions = createCombinedDailyChart(data)
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
}
