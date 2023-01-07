import { Component, Input } from '@angular/core';
import { BrokerageOrder, PositionInstance, StocksService, StrategyProfitPoint } from '../../services/stocks.service';

@Component({
  selector: 'app-stock-trading-position',
  templateUrl: './stock-trading-position.component.html',
  styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0
    _position: PositionInstance;

    positionProfitPoints : StrategyProfitPoint[] = []

    @Input()
    set position(v:PositionInstance) {
        this._position = v
        this.setCandidateValues()
    }

    @Input()
    pendingOrders: BrokerageOrder[]

    // constructor that takes stock service
    constructor(
        private stockService:StocksService
    ) {}

    fetchProfitPoints() {
        this.stockService.getStrategyProfitPoints(this._position.ticker, this._position.positionId).subscribe(
            (profitPoints) => {
                this.positionProfitPoints = profitPoints
            }
        )
    }
    
    setCandidateValues() {
        this.candidateRiskAmount = this._position.riskedAmount
        this.candidateStopPrice = this._position.stopPrice
    }

    recalculateRiskAmount() {
        var newRiskAmount = (this._position.averageCostPerShare - this.candidateStopPrice) * this._position.numberOfShares
        this.candidateRiskAmount = newRiskAmount
        this._position.riskedAmount = newRiskAmount
    }

    setStopPrice() {
        this.stockService.setStopPrice(this._position.ticker, this.candidateStopPrice).subscribe(
            (_) => {
                this._position.stopPrice = this.candidateStopPrice
            }
        )
    }

    deleteStopPrice() {
        this.stockService.deleteStopPrice(this._position.ticker).subscribe(
            (_) => {
                this._position.stopPrice = null
                this._position.riskedAmount = null
            }
        )
    }

    setRiskAmount() {
        this.stockService.setRiskAmount(this._position.ticker, this.candidateRiskAmount).subscribe(
            (_) => {
                this._position.riskedAmount = this.candidateRiskAmount
            }
        )
    }

    getPendingOrders(p:PositionInstance) {

        if (this.pendingOrders) {
            return  this.pendingOrders
                .filter(o => o.ticker == p.ticker)
                .filter(o => o.status != "FILLED" && o.status != "REPLACED")
        }
    }
}

