import { Component, Input } from '@angular/core';
import { Prices, StocksService, PositionInstance, TickerOutcomes, TradingStrategyResults } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-position-charts',
  templateUrl: './stock-position-charts.component.html',
  styleUrls: ['./stock-position-charts.component.css']
})
export class StockPositionChartsComponent {

  private _positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  simulationResults: TradingStrategyResults
  prices: Prices
  outcomes: TickerOutcomes;

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
    this.currentPosition = this.positions[this._index]
    // get price data and pass it to chart
    this.getSimulatedTrades();
  }

  private getSimulatedTrades() {
    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (results: TradingStrategyResults) => {
        this.simulationResults = results
        this.getPricesForCurrentPosition()
      }
    )
  }

  private getPricesForCurrentPosition() {
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        // only take the last 365 of prices
        this.prices = r;
      }
    );
  }

  next() {
    this._index++
    if (this._index >= this.positions.length) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

  keydownHandler($event: KeyboardEvent) {
    if ($event.key === 'ArrowRight')
    {
      this.next()
    }
    else if ($event.key === 'ArrowLeft')
    {
      this.previous()
    }
  }

  previous() {
    this._index--
    if (this._index < 0) {
      this._index = this.positions.length - 1
    }
    this.updateCurrentPosition()
  }

  buys(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'buy')
  }

  sells(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'sell')
  }

  linesOfInterest(positionInstance:PositionInstance) : number[] {
    var arr = [positionInstance.averageCostPerShare]
    if (positionInstance.stopPrice) {
      arr.push(positionInstance.stopPrice)
    }
    return arr
  }

  interestingOutcomes(a: TickerOutcomes): any {
    return a.outcomes.filter(o => o.type == 'Positive' || o.type == 'Negative')
  }

}
