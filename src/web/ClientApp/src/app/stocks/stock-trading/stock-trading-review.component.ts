import { Component, Input } from '@angular/core';
import { Prices, StocksService, PositionInstance, TradingStrategyResults } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent {
  
  private _positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  currentPositionPrice: number
  simulationResults: TradingStrategyResults
  prices: Prices

  constructor (private stockService: StocksService) { }

  @Input()
  set positions(positions:PositionInstance[]) {
    this._positions = positions
    this._index = 0
    this.updateCurrentPosition()
  }
  get positions() {
    return this._positions
  }

  updateCurrentPosition() {
    this.currentPosition = this._positions[this._index]
    // get price data and pass it to chart
    this.runTradingStrategies();
    this.fetchPrice();
  }

  private fetchPrice() {
    this.stockService.getStockPrice(this.currentPosition.ticker).subscribe(
      (r: number) => {
        this.currentPositionPrice = r;
      }
    );
  }

  private runTradingStrategies() {
    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (r: TradingStrategyResults) => {
        this.simulationResults = r;
        this.getPrices();
      }
    );
  }

  private getPrices() {
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        this.prices = r;
      }
    );
  }

  dropdownClick(elem: EventTarget) {
    var index = Number.parseInt((elem as HTMLInputElement).value)
    this._index = index
    this.updateCurrentPosition()
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
