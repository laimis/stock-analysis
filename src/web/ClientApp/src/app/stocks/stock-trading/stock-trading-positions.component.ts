import { Component, Input } from '@angular/core';
import { BrokerageOrder, PositionInstance, StocksService, toggleVisuallyHidden } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading-positions',
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
        this._positions = input
        this.updatePositions()
    }

    @Input()
    pendingOrders: BrokerageOrder[]

    // constructor that takes stock service
    constructor(
        private stockService:StocksService
    ) {}
    
    toggleVisibility(elem:HTMLElement) {
        console.log(elem)
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

    renderStyle: string = "card" // other style is "table"
    toggleRenderStyle() {
        if (this.renderStyle == "card") {
            this.renderStyle = "table"
        } else {
            this.renderStyle = "card"
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
                this.metricFunc = (p:PositionInstance) => p.unrealizedGainPct * 100
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
            console.log(this.metricFunc(a), this.metricFunc(b))
            if (Number.isFinite(this.metricFunc(a))) {
                return this.metricFunc(b) - this.metricFunc(a)
            }
            return String(this.metricFunc(a)).localeCompare(String(this.metricFunc(b)))
        })
    }
}

