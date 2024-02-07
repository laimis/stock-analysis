import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from "@angular/router";
import {OutcomesReport, StocksService, TickerOutcomes} from "../../services/stocks.service";
import {GetErrors} from "../../services/utils";

// this function will return tickers that match the new low trade candidates for buy side

export type TradeRecommendation = {
    recommendation: string
    matchingOutcomes: TickerOutcomes[]
} 

function getNewLowTradeCandidates(report:OutcomesReport): TradeRecommendation {
    let matchingOutcomes = report.outcomes.filter(outcome => {
        let currentPrice = outcome.outcomes.find(o => o.key === 'CurrentPrice').value
        let sma200 = outcome.outcomes.find(o => o.key === 'sma_200').value
        let sma20 = outcome.outcomes.find(o => o.key === 'sma_20').value
        
        return currentPrice > sma200 && currentPrice > sma20
    })
    
    // return trade recommendation instance
    return {
        recommendation: 'Buy New Low when current price is above sma200 and sma20',
        matchingOutcomes: matchingOutcomes
    }
}

function selectFunctionToUse(screenerId:string) {
    switch (screenerId) {
        case '31':
            return getNewLowTradeCandidates
        default:
            return (_:OutcomesReport) => {
                return {
                    recommendation: 'No recommendation',
                    matchingOutcomes: []
                }
            }
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
    tradeRecommendation: TradeRecommendation
    
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
        
        let func = selectFunctionToUse(this.screenerId)
        
        // multiple bar daily report
        this.service.reportOutcomesAllBars(this.tickers).subscribe(
            report => {
                console.log(report)
                this.tradeRecommendation = func(report)
            },
            error => {
                this.errors = GetErrors(error)
            }
        )
    }
}
