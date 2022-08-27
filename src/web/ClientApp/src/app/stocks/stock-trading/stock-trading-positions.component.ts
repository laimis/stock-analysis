import { Component, Input } from '@angular/core';
import { BrokerageOrder, StockTradingPosition, toggleVisuallHidden } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionComponent {
    sortedPositions: StockTradingPosition[];
    _positions: StockTradingPosition[];
    metricToRender: string = "rr"
    metricFunc: (p: StockTradingPosition) => number = (p:StockTradingPosition) => p.unrealizedRR;

    @Input()
    set positions(input: StockTradingPosition[]) {
        this._positions = input
        this.updatePositions()
    }

    @Input()
    pendingOrders: BrokerageOrder[]

    @Input()
    stopLoss: number

    @Input()
    firstTarget: number

    @Input()
    rrTarget: number

    stopPrice(p:StockTradingPosition) {
        if (p.stopPrice) {
            return p.stopPrice
        }
        return p.averageCost - p.averageCost * this.stopLoss
    }

    firstTargetNumber(p:StockTradingPosition) {
        return p.averageCost + p.averageCost * p.riskedPct
    }

    secondTargetNumber(p:StockTradingPosition) {
        return p.averageCost + p.averageCost * 2.5 * p.riskedPct
    }

    positionProgress(p:StockTradingPosition) {
        return p.numberOfShares * 1.0 / p.maxNumberOfShares * 100
    }

    positionSize(p:StockTradingPosition) {
        return p.numberOfShares * p.averageCost
    }

    tradingPortion(p:StockTradingPosition) {
        return Math.floor(p.maxNumberOfShares / 3)
    }

    toggleVisibility(elem:HTMLElement) {
        console.log(elem)
        toggleVisuallHidden(elem)
    }

    getPendingOrders(p:StockTradingPosition) {
        return this.pendingOrders
            .filter(o => o.ticker == p.ticker)
            .filter(o => o.status != "FILLED" && o.status != "REPLACED")
    }

    metricChanged(elem: EventTarget) {
        var value = (elem as HTMLInputElement).value
        
        this.metricToRender = value

        switch (value) {
            case "pl":
                this.metricFunc = (p:StockTradingPosition) => p.unrealizedGain
                break;
            case "plPercent":
                this.metricFunc = (p:StockTradingPosition) => p.unrealizedGainPct
                break;
            case "cost":
                this.metricFunc = (p:StockTradingPosition) => this.positionSize(p)
                break;
            default:
                this.metricFunc = (p:StockTradingPosition) => p.unrealizedRR
        }

        this.updatePositions()
    }

    getMetricToRender(p:StockTradingPosition) {
        return this.metricFunc(p)
    }

    updatePositions() {
        this.sortedPositions = this._positions.sort((a, b) => {
            return this.metricFunc(b) - this.metricFunc(a)
        })
    }
}

