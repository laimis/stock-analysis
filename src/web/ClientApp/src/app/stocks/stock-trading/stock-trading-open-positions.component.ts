import { Component, Input } from '@angular/core';
import {
  Prices,
  StocksService,
  PositionInstance,
  TickerOutcomes,
  TradingStrategyResults,
  BrokerageOrder,
  DailyOutcomeScoresReport,
  PriceWithDate,
  DailyPositionReport
} from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-open-positions',
  templateUrl: './stock-trading-open-positions.component.html',
  styleUrls: ['./stock-trading-open-positions.component.css']
})
export class StockTradingOpenPositionsComponent {

  private _positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  simulationResults: TradingStrategyResults
  prices: Prices
  outcomes: TickerOutcomes;
  dailyScores: DailyPositionReport;
  positionBuys: string[]
  positionSells: string[]

  constructor (private stockService: StocksService) { }

  @Input()
  set positions(positions:PositionInstance[]) {
    this._positions = positions // positions.filter(p => p.isShortTerm)
    this._index = 0
    this.updateCurrentPosition()
  }
  get positions() {
    return this._positions
  }

  @Input()
  orders:BrokerageOrder[]

  updateCurrentPosition() {
    this.currentPosition = this.positions[this._index]
    this.dailyScores = null
    this.prices = null
    this.simulationResults = null
    this.positionBuys = this.currentPosition.transactions.filter(t => t.type == 'buy').map(t => t.date)
    this.positionSells = this.currentPosition.transactions.filter(t => t.type == 'sell').map(t => t.date)
    
    // get price data and pass it to chart
    this.getSimulatedTrades();
  }

  private getSimulatedTrades() {
    this.simulationResults = null

    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (results: TradingStrategyResults) => {
        this.simulationResults = results
        this.getScores()
      },
      (error) => {
        console.log("error fetching simulations: " + error)
        this.getPricesForCurrentPosition()
      }
    )
  }

  private getPricesForCurrentPosition() {
    // only take the last 365 of prices
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        this.prices = r;
      }
    );
  }

  private getScores() {
    this.stockService.reportDailyPositionReport(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (r: DailyPositionReport) => {
        this.dailyScores = r;
        this.getPricesForCurrentPosition()
      }
    );
  }

  dropdownClick(elem: EventTarget) {
    var index = Number.parseInt((elem as HTMLInputElement).value)
    console.log("dropdown click " + index)
    this._index = index
    this.updateCurrentPosition()
  }

  next() {
    this._index++
    if (this._index >= this.positions.length) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

  previous() {
    this._index--
    if (this._index < 0) {
      this._index = this.positions.length - 1
    }
    this.updateCurrentPosition()
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
