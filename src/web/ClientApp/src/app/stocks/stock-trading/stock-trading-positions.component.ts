import { Component, Input } from '@angular/core';
import { BrokerageOrder, PositionInstance, toggleVisuallHidden } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionComponent {
    sortedPositions: PositionInstance[];
    _positions: PositionInstance[];
    metricToRender: string = "rr"
    metricFunc: (p: PositionInstance) => any = (p:PositionInstance) => p.unrealizedRR;

    @Input()
    set positions(input: PositionInstance[]) {
        this._positions = input
        this.updatePositions()
    }

    @Input()
    pendingOrders: BrokerageOrder[]

    
    toggleVisibility(elem:HTMLElement) {
        console.log(elem)
        toggleVisuallHidden(elem)
    }

    getPendingOrders(p:PositionInstance) {
        return this.pendingOrders
            .filter(o => o.ticker == p.ticker)
            .filter(o => o.status != "FILLED" && o.status != "REPLACED")
    }

    metricChanged(elem: EventTarget) {
        var value = (elem as HTMLInputElement).value
        
        this.metricToRender = value

        switch (value) {
            case "pl":
                this.metricFunc = (p:PositionInstance) => p.unrealizedProfit
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
            default:
                this.metricFunc = (p:PositionInstance) => p.unrealizedRR
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

