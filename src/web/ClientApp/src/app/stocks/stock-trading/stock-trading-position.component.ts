import { Component, Input } from '@angular/core';
import { BrokerageOrder, PositionInstance, StocksService, toggleVisuallHidden } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-position',
  templateUrl: './stock-trading-position.component.html',
  styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0

    @Input()
    position : PositionInstance

    @Input()
    metricFunc: (p:PositionInstance) => number

    @Input()
    pendingOrders: BrokerageOrder[]

    // constructor that takes stock service
    constructor(
        private stockService:StocksService
    ) {}
    
    toggleVisibility(elem:HTMLElement) {
        console.log(elem)
        toggleVisuallHidden(elem)
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

    getPendingOrders(p:PositionInstance) {
        return this.pendingOrders
            .filter(o => o.ticker == p.ticker)
            .filter(o => o.status != "FILLED" && o.status != "REPLACED")
    }

    getMetricToRender(p:PositionInstance) {
        var val = this.metricFunc(p)
        if (Number.isFinite(val)) {
            return Math.round(val * 100) / 100
        }
        return val
    }
}

