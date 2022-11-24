import { Component, OnInit, Input } from '@angular/core';
import { Prices, StocksService, PositionInstance } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent implements OnInit {
  ngOnInit() {
  }

  private _positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  simulatedPosition: PositionInstance
  prices: Prices

  constructor (private stockService: StocksService) { }

  @Input()
  set positions(positions:PositionInstance[]) {
    this._positions = positions
    this._index = 0
    this.updateCurrentPosition()
  }

  updateCurrentPosition() {
    this.currentPosition = this._positions[this._index]
    // get price data and pass it to chart
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        this.prices = r
      }
    )
    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (r: PositionInstance) => {
        this.simulatedPosition = r
      }
    )
  }

  next() {
    this._index++
    if (this._index >= this._positions.length) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

  previous() {
    this._index--
    if (this._index < 0) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

  buys(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'buy')
  }

  sells(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'sell')
  }

}
