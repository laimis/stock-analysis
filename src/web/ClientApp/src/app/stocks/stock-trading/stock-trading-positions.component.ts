import { Component, Input, OnInit } from '@angular/core';
import { StockTradingGridEntry } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionComponent {
    @Input()
    positions: StockTradingGridEntry[]

    @Input()
    stopLoss: number

    @Input()
    firstTarget: number

    @Input()
    rrTarget: number

    gain(p:StockTradingGridEntry) {
        return (p.price - p.averageCost) / p.averageCost
    }

    stopNumber(p:StockTradingGridEntry) {
        return p.averageCost - p.averageCost * this.stopLoss
    }

    firstTargetNumber(p:StockTradingGridEntry) {
        return p.averageCost + p.averageCost * this.firstTarget
    }

    rrTargetNumber(p:StockTradingGridEntry) {
        return p.averageCost + p.averageCost * this.rrTarget
    }

    positionProgress(p:StockTradingGridEntry) {
        return p.numberOfShares * 1.0 / p.maxNumberOfShares * 100
    }

    positionSize(p:StockTradingGridEntry) {
        return p.maxNumberOfShares * p.averageCost
    }
}

