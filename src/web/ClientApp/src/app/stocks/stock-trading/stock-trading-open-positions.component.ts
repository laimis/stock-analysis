import {Component, HostListener, Input} from '@angular/core';
import {
  BrokerageOrder, ChartMarker,
  DailyPositionReport,
  PositionChartInformation,
  PositionInstance,
  PriceFrequency,
  Prices,
  StocksService,
  TickerOutcomes,
  TradingStrategyResults
} from 'src/app/services/stocks.service';
import {green, red} from "../../shared/candlestick-chart/candlestick-chart.component";


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
  outcomes: TickerOutcomes;
  dailyScores: DailyPositionReport;
  chartInfo: PositionChartInformation;
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
    this.chartInfo = null
    this.simulationResults = null

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
    this.stockService.getStockPrices(this.currentPosition.ticker, 365, PriceFrequency.Daily).subscribe(
      (r: Prices) => {

        let markers : ChartMarker[] = []
        let buyTxs = this.currentPosition.transactions.filter(t => t.type == 'buy')
        let sellTxs = this.currentPosition.transactions.filter(t => t.type == 'sell')

        if (buyTxs.length > 0) {
          markers.push({date: buyTxs[0].date, label: 'Buy', color: green, shape: 'arrowUp'})
        }

        if (sellTxs.length > 0) {
          markers.push({date: sellTxs[0].date, label: 'Sell', color: red, shape: 'arrowDown'})
        }

        this.chartInfo = {
          markers: markers,
          ticker: this.currentPosition.ticker,
          prices: r,
          averageBuyPrice: this.currentPosition.averageCostPerShare,
          stopPrice: this.currentPosition.stopPrice
        };
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
    let index = Number.parseInt((elem as HTMLInputElement).value)
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

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {

    if (event.key === "ArrowRight") {
      this.next();
      event.preventDefault();
    } else if (event.key === "ArrowLeft") {
      this.previous();
      event.preventDefault();
    }
  }

}
