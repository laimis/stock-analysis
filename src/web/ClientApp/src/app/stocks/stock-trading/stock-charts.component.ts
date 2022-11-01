import { Component, OnInit, Input } from '@angular/core';
import { Prices, StocksService, PositionInstance, TickerOutcomes } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-charts',
  templateUrl: './stock-charts.component.html',
  styleUrls: ['./stock-charts.component.css']
})
export class StockChartsComponent implements OnInit {


  ngOnInit() {
  }

  private _positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  prices: Prices
  outcomes: TickerOutcomes;

  constructor (private stockService: StocksService) { }

  @Input()
  set positions(positions:PositionInstance[]) {
    this._positions = positions
    this._index = 0
    this.updateCurrentPosition()
  }

  metricFunc: (p: PositionInstance) => any = (p:PositionInstance) => p.unrealizedRR;

  updateCurrentPosition() {
    this.currentPosition = this._positions[this._index]
    // get price data and pass it to chart
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        // only take the last 365 of prices
        this.prices = r
      }
    )

    this.outcomes = null
    this.stockService.reportTickerOutcomesAllTime(this.currentPosition.ticker, false).subscribe(
      data => {
        this.outcomes = data[0]
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
      this._index = this._positions.length - 1
    }
    this.updateCurrentPosition()
  }

  buys(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'buy')
  }

  sells(positionInstance:PositionInstance) {
    return positionInstance.transactions.filter(t => t.type == 'sell')
  }

  interestingOutcomes(a: TickerOutcomes): any {
    return a.outcomes.filter(o => o.type == 'Positive' || o.type == 'Negative')
  }

}
