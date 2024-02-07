import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {OutcomesReport, StockAnalysisOutcome, StocksService, TickerOutcomes} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";

// this function will return tickers that match the new low trade candidates for buy side

export type TradeRecommendation = {
    recommendation: string
    matchingOutcomes: TickerOutcomes[]
}

function firstOutcomeMatchByKey(outcome: TickerOutcomes, key: string): StockAnalysisOutcome {
    return outcome.outcomes.find(o => o.key === key)
}

function newLowBuyWhenPriceAbove20AND200(report:OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        let sma20 = firstOutcomeMatchByKey(outcome, 'sma_20').value
        return currentPrice > sma200 && currentPrice > sma20
    })
    
    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when current price is above sma200 AND sma20',
        matchingOutcomes: matchingOutcomes
    }
}

function newLowBuyWhenPriceAbove200(report:OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = firstOutcomeMatchByKey(outcome, 'CurrentPrice').value
        let sma200 = firstOutcomeMatchByKey(outcome, 'sma_200').value
        return currentPrice > sma200
    })
    
    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when current price is above sma200',
        matchingOutcomes: matchingOutcomes
    }
}


function selectFunctionsToUse(screenerId:string) {
    switch (screenerId) {
        case '31':
            return [newLowBuyWhenPriceAbove20AND200, newLowBuyWhenPriceAbove200]
        default:
            return [
                (_:OutcomesReport) => {
                    return {
                        recommendation: 'No recommendation',
                        matchingOutcomes: []
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
    
    constructor(private route: ActivatedRoute, private service: StocksService) {}
    
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
        
        this.tickers = this.route.snapshot.queryParamMap.get('tickers').split(',')
        
        let funcs = selectFunctionsToUse(this.screenerId)
        
        // multiple bar daily report
        this.service.reportOutcomesAllBars(this.tickers).subscribe(
            report => {
                console.log(report)
                funcs.forEach(func => {
                    this.tradeRecommendations.push(func(report))
                })
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }
}
