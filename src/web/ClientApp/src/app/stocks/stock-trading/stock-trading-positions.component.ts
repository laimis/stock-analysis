import { Component, Input, OnInit } from '@angular/core';
import { StockTradingPosition } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css']
})
export class StockTradingPositionComponent {
    @Input()
    positions: StockTradingPosition[]

    @Input()
    stopLoss: number

    @Input()
    firstTarget: number

    @Input()
    rrTarget: number

    gain(p:StockTradingPosition) {
        return (p.price - p.averageCost) / p.averageCost
    }

    stopNumber(p:StockTradingPosition) {
        return p.averageCost - p.averageCost * this.stopLoss
    }

    firstTargetNumber(p:StockTradingPosition) {
        return p.averageCost + p.averageCost * this.firstTarget
    }

    rrTargetNumber(p:StockTradingPosition) {
        return p.averageCost + p.averageCost * this.rrTarget
    }

    positionProgress(p:StockTradingPosition) {
        return p.numberOfShares * 1.0 / p.maxNumberOfShares * 100
    }

    positionSize(p:StockTradingPosition) {
        return p.maxNumberOfShares * p.averageCost
    }
}

