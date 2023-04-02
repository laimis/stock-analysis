import { Component, Input } from '@angular/core';
import { toggleVisuallyHidden } from 'src/app/services/utils';
import { PositionInstance, StocksService } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionsComponent {
    sortedPositions: PositionInstance[];
    _positions: PositionInstance[];
    metricToRender: string = "rr"
    metricFunc: (p: PositionInstance) => any = (p:PositionInstance) => p.rr;
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0

    @Input()
    set positions(input: PositionInstance[]) {
        this._positions = input.filter(p => p.isShortTerm)
        this.updatePositions()
    }

    // constructor that takes stock service
    constructor(
        private stockService:StocksService
    ) {}
    
    toggleVisibility(elem:HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    setCandidateValues(p:PositionInstance) {
        this.candidateRiskAmount = p.riskedAmount
        this.candidateStopPrice = p.stopPrice
    }

    recalculateRiskAmount(p:PositionInstance) {
        var newRiskAmount = (p.averageCostPerShare - this.candidateStopPrice) * p.numberOfShares
        this.candidateRiskAmount = newRiskAmount
        p.riskedAmount = newRiskAmount
    }

    setStopPrice(p:PositionInstance) {
        this.stockService.setStopPrice(p.ticker, this.candidateStopPrice).subscribe(
            (_) => {
                p.stopPrice = this.candidateStopPrice
            }
        )
    }

    setRiskAmount(p:PositionInstance) {
        this.stockService.setRiskAmount(p.ticker, this.candidateRiskAmount).subscribe(
            (_) => {
                p.riskedAmount = this.candidateRiskAmount
            }
        )
    }

    renderStyle: string = "card"  // other style is "table"
    renderStyleName: string = "Switch to table layout"
    toggleRenderStyle() {
        if (this.renderStyle == "card") {
            this.renderStyle = "table"
            this.renderStyleName = "Switch to card layout"
        } else {
            this.renderStyle = "card"
            this.renderStyleName = "Switch to table layout"
        }
    }

    metricChanged(elem: EventTarget) {
        var value = (elem as HTMLInputElement).value
        
        this.metricToRender = value

        switch (value) {
            case "pl":
                this.metricFunc = (p:PositionInstance) => p.profit
                break;
            case "plPercent":
                this.metricFunc = (p:PositionInstance) => p.unrealizedGainPct
                break;
            case "cost":
                this.metricFunc = (p:PositionInstance) => p.cost
                break;
            case "ticker":
                this.metricFunc = (p:PositionInstance) => p.ticker
                break
            case "daysSinceLastTransaction":
                this.metricFunc = (p:PositionInstance) => p.daysSinceLastTransaction
                break
            case "percentToStop":
                this.metricFunc = (p:PositionInstance) => p.percentToStop * 100
                break
            case "riskedAmount":
                this.metricFunc = (p:PositionInstance) => p.riskedAmount ? p.riskedAmount : 0
                break
            case "daysHeld":
                this.metricFunc = (p:PositionInstance) => p.daysHeld
                break
            case "unrealizedPL":
                this.metricFunc = (p:PositionInstance) => p.unrealizedProfit
                break
            case "combinedPL":
                this.metricFunc = (p:PositionInstance) => p.combinedProfit
                break
            default:
                this.metricFunc = (p:PositionInstance) => p.rr
        }

        this.updatePositions()
    }

    getMetricToRender(p:PositionInstance) {
        var val = this.metricFunc(p)
        if (Number.isFinite(val)) {
            return Math.round(val * 100) / 100
        }
        return val
    }

    updatePositions() {
        this.sortedPositions = this._positions.sort((a, b) => {
            if (Number.isFinite(this.metricFunc(a))) {
                return this.metricFunc(b) - this.metricFunc(a)
            }
            return String(this.metricFunc(a)).localeCompare(String(this.metricFunc(b)))
        })
    }
}

