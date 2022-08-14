import { Component, Input } from '@angular/core';
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
        return p.maxNumberOfShares * p.averageCost
    }

    tradingPortion(p:StockTradingPosition) {
        return Math.floor(p.maxNumberOfShares / 3)
    }
}

