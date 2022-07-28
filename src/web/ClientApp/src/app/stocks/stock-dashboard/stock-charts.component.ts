import { Component, OnInit, Input } from '@angular/core';
import { Prices, StocksService, StockTradingPosition } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-charts',
  templateUrl: './stock-charts.component.html',
  styleUrls: ['./stock-charts.component.css']
})
export class StockChartsComponent implements OnInit {
  ngOnInit() {
  }

  private _positions: StockTradingPosition[]
  private _index: number = 0
  currentPosition: StockTradingPosition
  prices: Prices

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
        this.prices = r
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
