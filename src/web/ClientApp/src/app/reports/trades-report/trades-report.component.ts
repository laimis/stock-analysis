import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {OutcomesReport, StockAnalysisOutcome, StocksService, TickerOutcomes} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";
import {Title} from "@angular/platform-browser";

// this function will return tickers that match the new low trade candidates for buy side

export type TradeRecommendation = {
    recommendation: string
    matchingOutcomes: TickerOutcomes[]
    notes: string[]
    cycles: string[]
}

function firstOutcomeMatchByKey(outcome: TickerOutcomes, key: string): StockAnalysisOutcome {
    return outcome.outcomes.find(o => o.key === key)
}


// NEW HIGH
function newHighBuyWhenGapUp(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let gapUp = firstOutcomeMatchByKey(outcome, 'GapPercentage').value
        return gapUp > 0
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy New High when Gap Up into it',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: [
            'My Cycle DOWN and UP',
            'SPY ST DOWN and UP',
            'SPY LT DOWN and UP'
        ]
    }
}

function newHighBuyWhenPriceAbove200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice > sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy New High when price is above sma200',
        matchingOutcomes: matchingOutcomes,
        notes: [''],
        cycles: ['SPY LT UP']
    }
}

// NEW HIGH END

// TOP GAINER
function topGainerBuyWhenPriceAbove200AndSpySTDown(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice > sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy Top Gainer when price is above sma200 and SPY ST DOWN',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: ['SPY ST DOWN']
    }
}

function topGainerShortWhenPriceBelow200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short Top Gainer when price is below sma200',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: ['All']
    }
}

function topGainerShortWhenPriceBelowSMAAlign(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        let sma50 = firstOutcomeMatchByKey(outcome, 'sma_50').value
        let sma150 = firstOutcomeMatchByKey(outcome, 'sma_150').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma20 && sma20 < sma50 && sma50 < sma150 && sma150 < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short Top Gainer when price is < SMAAlign',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: [
            'My Cycle UP/DOWN',
            'SPY ST DOWN',
            'SPY LT DOWN'
        ]
    }
}

// TOP GAINER END

// TOP LOSER
function topLoserBuyWhenPriceAbove20(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        return currentPrice > sma20
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy Top Loser when price is above sma20',
        matchingOutcomes: matchingOutcomes,
        notes: ['Stop Loss friendly in particularly in SPY LT UP cycle'],
        cycles: ['All']
    }
}

function topLoserBuyWhenPriceAbove200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        return currentPrice > sma200 && currentPrice < sma20
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy Top Loser when price is above sma200 (but under 20sma)',
        matchingOutcomes: matchingOutcomes,
        notes: ['Stop Loss friendly in particularly in SPY LT UP cycle'],
        cycles: ['All']
    }
}

function topLoserBuyWhenPriceAboveSMAAlign(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        let sma50 = firstOutcomeMatchByKey(outcome, 'sma_50').value
        let sma150 = firstOutcomeMatchByKey(outcome, 'sma_150').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice > sma20 && sma20 > sma50 && sma50 > sma150 && sma150 > sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy Top Loser when price is > SMAAlign',
        matchingOutcomes: matchingOutcomes,
        notes: ['Stop Loss friendly in SPY LT UP'],
        cycles: ['My Cycle UP']
    }
}

function topLoserShortWhenPriceBelow200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short Top Loser when price is below sma200',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: [
            'My Cycle UP',
            'SPY ST UP',
            'SPY LT DOWN'
        ]
    }
}

function topLoserShortWhenPriceBelowSMAAlign(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        let sma50 = firstOutcomeMatchByKey(outcome, 'sma_50').value
        let sma150 = firstOutcomeMatchByKey(outcome, 'sma_150').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma20 && sma20 < sma50 && sma50 < sma150 && sma150 < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short Top Loser when price is < SMAAlign',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: [
            'My Cycle UP/DOWN',
            'SPY ST UP',
            'SPY LT DOWN'
        ]
    }
}

// TOP LOSER END

// NEW LOW
function newLowBuyWhenPriceAbove20AND200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        return currentPrice > sma200 && currentPrice > sma20
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when price is above sma200 AND sma20',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: ['All']
    }
}

function newLowBuyWhenPriceAbove200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice > sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when price is above sma200',
        matchingOutcomes: matchingOutcomes,
        notes: ['Stop Loss friendly in particularly in SPY LT UP cycle'],
        cycles: ['All']
    }
}

function newLowBuyWhenGapDown(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let gapDown = firstOutcomeMatchByKey(outcome, 'GapPercentage').value
        return gapDown < 0
    })

    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when Gap Down into it',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: [
            'My Cycle DOWN',
            'SPY ST DOWN',
            'SPY LT DOWN <-- suspect, maybe because of super recovery plays? need more investigation.'
        ]
    }
}

function newLowShortWhenPriceBelow200(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short New Low when price is below sma200',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: ['My Cycle UP', 'SPY LT DOWN']
    }
}

function newLowShortWhenPriceBelowSMAAlign(report: OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        let sma50 = firstOutcomeMatchByKey(outcome, 'sma_50').value
        let sma150 = firstOutcomeMatchByKey(outcome, 'sma_150').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice < sma20 && sma20 < sma50 && sma50 < sma150 && sma150 < sma200
    })

    // return trade recommendation instance
    return {
        recommendation: 'Short New Low when price is < SMAAlign',
        matchingOutcomes: matchingOutcomes,
        notes: [],
        cycles: ['My Cycle UP', 'SPY ST UP']
    }
}

// NEW LOW END


function selectFunctionsToUse(screenerId: string) {
    switch (screenerId) {
        case '28':
            return [newHighBuyWhenGapUp, newHighBuyWhenPriceAbove200]
        case '29':
            return [topGainerBuyWhenPriceAbove200AndSpySTDown, topGainerShortWhenPriceBelowSMAAlign, topGainerShortWhenPriceBelow200]
        case '30':
            return [topLoserBuyWhenPriceAbove20, topLoserBuyWhenPriceAbove200, topLoserBuyWhenPriceAboveSMAAlign, topLoserShortWhenPriceBelowSMAAlign, topLoserShortWhenPriceBelow200]
        case '31':
            return [newLowBuyWhenPriceAbove20AND200, newLowBuyWhenPriceAbove200, newLowBuyWhenGapDown, newLowShortWhenPriceBelowSMAAlign, newLowShortWhenPriceBelow200]

        default:
            return [
                (_: OutcomesReport) => {
                    return {
                        recommendation: 'No recommendation',
                        matchingOutcomes: [],
                        stopLossFriendly: false,
                        cycles: []
                    }
                }
            ]
    }
}

@Component({
    selector: 'app-trades-report',
    templateUrl: './trades-report.component.html',
    styleUrl: './trades-report.component.css'
})
export class TradesReportComponent implements OnInit {
    tickers: string[];
    errors: string[];
    screenerId: string;
    tradeRecommendations: TradeRecommendation[] = []

    constructor(
        private route: ActivatedRoute,
        private service: StocksService,
        private title: Title) {
    }

    ngOnInit() {
        // we must get screenerId parameter from the route, if it's not there, set the error informing the user
        if (!this.route.snapshot.queryParamMap.has('screenerId')) {
            this.errors = ['Screener Id is missing']
            return
        }

        this.screenerId = this.route.snapshot.queryParamMap.get('screenerId')

        // we must get tickers parameter from the route, if it's not there, set the error informing the user
        if (!this.route.snapshot.queryParamMap.has('tickers')) {
            this.errors = ['No tickers provided']
            return
        }

        const title = this.route.snapshot.queryParamMap.get('title') ?? this.screenerId

        this.title.setTitle("Trades Report: " + title)

        this.tickers = this.route.snapshot.queryParamMap.get('tickers').split(',')

        let funcs = selectFunctionsToUse(this.screenerId)

        // multiple bar daily report
        this.service.reportOutcomesAllBars(this.tickers).subscribe(
            report => {
                console.log(report)
                funcs.forEach(func => this.tradeRecommendations.push(func(report)))
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }
}
