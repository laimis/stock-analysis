import { Component, OnInit, Input } from '@angular/core';
import { Prices, StockHistoricalPrice, StocksService, StockTradingPosition } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent implements OnInit {
  ngOnInit() {
  }

  private _positions: StockTradingPosition[]
  private _index: number = 0
  currentPosition: StockTradingPosition
  prices: StockHistoricalPrice[]

  constructor (private stockService: StocksService) { }

  @Input()
  set positions(positions:StockTradingPosition[]) {
    this._positions = positions
    this._index = 0
    this.updateCurrentPosition()
  }

  updateCurrentPosition() {
    this.currentPosition = this._positions[this._index]
    // get price data and pass it to chart
    this.stockService.getStockPrices2y(this.currentPosition.ticker).subscribe(
      (r: Prices) => {
        this.prices = r.prices
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

}
