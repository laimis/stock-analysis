import { Component, OnInit } from '@angular/core';
import { Prices, StocksService, PositionInstance, TradingStrategyResults, StockTradingPositions, DailyOutcomeScoresReport } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent implements OnInit {
  
  positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  currentPositionPrice: number
  simulationResults: TradingStrategyResults
  prices: Prices
  dailyScores: DailyOutcomeScoresReport;

  constructor (private stockService: StocksService) { }

  ngOnInit(): void {
    this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
        this.positions = r.past
        this.updateCurrentPosition()
      }, (error) => {
        console.log("error fetching positions: " + error)
      }
    );
  }

  updateCurrentPosition() {
    this.currentPosition = this.positions[this._index]
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

  private getScores() {
    this.stockService.reportDailyOutcomesReport(
      this.currentPosition.ticker,
      this.currentPosition.opened.split('T')[0],
      this.currentPosition.closed.split('T')[0]).subscribe(
      (r: DailyOutcomeScoresReport) => {
        this.dailyScores = r;
        this.getPrices();
      }
    );
  }

  private runTradingStrategies() {
    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (r: TradingStrategyResults) => {
        this.simulationResults = r;
        this.getScores()
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
    if (this._index >= this.positions.length) {
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
