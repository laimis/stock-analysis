import { Component, Input, OnInit } from '@angular/core';
import { StockTradingGridEntry } from '../services/stocks.service';

@Component({
  selector: 'app-trading-position',
  templateUrl: './trading-position.component.html',
  styleUrls: ['./trading-position.component.css']
})
export class TradingPositionComponent {
    @Input()
    p: StockTradingGridEntry

    @Input()
    stopLoss: number

    @Input()
    firstTarget: number

    @Input()
    rrTarget: number

    gain() {
        return (this.p.price - this.p.averageCost) / this.p.averageCost
    }

    stopNumber() {
        return this.p.averageCost - this.p.averageCost * this.stopLoss
    }

    firstTargetNumber() {
        return this.p.averageCost + this.p.averageCost * this.firstTarget
    }

    rrTargetNumber() {
        return this.p.averageCost + this.p.averageCost * this.rrTarget
    }

    positionProgress() {
        return this.p.numberOfShares * 1.0 / this.p.maxNumberOfShares * 100
    }
}

