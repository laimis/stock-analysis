import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {PositionInstance, StockQuote, StocksService, StockTradingPositions} from '../services/stocks.service';
import {GetErrors} from "../services/utils";
import {concat} from "rxjs";
import {tap} from "rxjs/operators";
import {StockPositionsService} from "../services/stockpositions.service";


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
    styleUrls: ['./playground.component.css']
})
export class PlaygroundComponent implements OnInit {
    tickers: string[];
    errors: string[];
    status: string;
    testTicker: string;
    chartOptions: any[] = []

    constructor(
        private stocks: StocksService,
        private stockPositions: StockPositionsService,
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        const tickerParam = this.route.snapshot.queryParamMap.get('tickers')
        this.tickers = tickerParam ? tickerParam.split(',') : ['AMD']
        this.testTicker = this.tickers[0]

        this.stockPositions.getTradingEntries().subscribe((data) => {
            this.createChartOptions(data)
        }, (error) => {
            this.errors = GetErrors(error)
        })
    }

    createChartOptions(positions: StockTradingPositions) {
        const entries = positions.current
        const quotes = positions.prices

        const profitScatter = createProfitScatter(entries, quotes)

        this.chartOptions.push(profitScatter)
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
