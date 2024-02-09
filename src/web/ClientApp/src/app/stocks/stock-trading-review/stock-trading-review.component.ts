import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
  BrokerageOrder,
  ChartMarker,
  DailyPositionReport,
  PositionChartInformation,
  PositionInstance,
  PriceFrequency,
  Prices,
  StockQuote, StocksService, StockTransaction,
  TradingStrategyResults
} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {green, red} from "../../shared/candlestick-chart/candlestick-chart.component";
import {StockPositionsService} from "../../services/stockpositions.service";


@Component({
  selector: 'app-stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent {

  private _index: number = 0
  currentPosition: PositionInstance
  simulationResults: TradingStrategyResults
  simulationErrors: string[];
  scoresErrors: string[];
  pricesErrors: string[]
  dailyPositionReport: DailyPositionReport
  private _positions: PositionInstance[];
  positionChartInformation: PositionChartInformation;

  constructor (
    private stockService: StocksService,
    private stockPositionsService: StockPositionsService,
    private title: Title) { }

  @Input()
  set positions(value: PositionInstance[]) {
    this._index = 0
    this._positions = value
    this.updateCurrentPosition()
  }
  get positions(): PositionInstance[] {
    return this._positions
  }

  @Input()
  quotes:object

  @Input()
  orders:BrokerageOrder[]
    
    @Output()
    brokerageOrdersChanged: EventEmitter<string> = new EventEmitter<string>()

  updateCurrentPosition() {
    this.currentPosition = this.positions[this._index]
    // get price data and pass it to chart
    this.gradingError = null
    this.gradingSuccess = null
    this.assignedGrade = this.currentPosition.grade
    this.assignedNote = this.currentPosition.gradeNote
    this.simulationResults = null
    this.dailyPositionReport = null
    this.runTradingStrategies(this.currentPosition);
    this.positionChartInformation = null
    this.setTitle();
  }

  private setTitle() {
    this.title.setTitle(`Trading Review - ${this.currentPosition.ticker} - Nightingale Trading`)
  }

  getPrice(position:PositionInstance) {
    let quote = this.getQuote(position)

    if (quote) {
      return quote.price
    }

    // check if we have prices perhaps available
    if (this.positionChartInformation && this.positionChartInformation.prices) {
      let prices = this.positionChartInformation.prices
      return prices.prices[prices.prices.length - 1].close
    }

    return 0
  }

  getQuote(position:PositionInstance) {
    if (this.quotes && position.ticker in this.quotes) {
      return this.quotes[position.ticker]
    }
    return null
  }

  private getPositionReport(position: PositionInstance) {
    this.stockService.reportDailyPositionReport(
      position.ticker,
      position.positionId).subscribe(
      (r: DailyPositionReport) => {
        this.scoresErrors = null
        this.dailyPositionReport = r;
        this.getPrices(position);
      },
      (error) => {
        this.dailyPositionReport = null
        this.scoresErrors = GetErrors(error)
        this.getPrices(position);
      }
    );
  }

  private runTradingStrategies(position: PositionInstance) {
    this.stockPositionsService.simulatePosition(position.positionId).subscribe(
      (r: TradingStrategyResults) => {
        this.simulationErrors = null
        this.simulationResults = r;
        this.getPositionReport(position)
      },
      (error) => {
        this.simulationErrors = GetErrors(error)
        this.getPositionReport(position)
      }
    );
  }

  private getPrices(position:PositionInstance) {
    this.stockService.getStockPrices(position.ticker, 365, PriceFrequency.Daily).subscribe(
      (r: Prices) => {

        let markers: ChartMarker[] = []

        position.transactions
          .filter((t: StockTransaction) => t.type == 'buy')
          .forEach((t: StockTransaction) => {
          markers.push({date: t.date, label: 'Buy ' + t.numberOfShares, color: green, shape: 'arrowUp'})
        })

        position.transactions
          .filter((t: StockTransaction) => t.type == 'sell')
          .forEach((t: StockTransaction) => {
            markers.push({date: t.date, label: 'Sell ' + t.numberOfShares, color: red, shape: 'arrowDown'})
          })

        this.pricesErrors = null
        this.positionChartInformation = {
          averageBuyPrice: position.averageCostPerShare,
          stopPrice: position.stopPrice,
          markers: markers,
          prices: r,
          ticker: position.ticker
        }
      },
      (error) => {
        this.positionChartInformation = null
        this.pricesErrors = GetErrors(error)
      }
    );
  }

  dropdownClick(elem: EventTarget) {
    this._index = Number.parseInt((elem as HTMLInputElement).value)
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

  gradingError: string = null
  gradingSuccess: string = null
  assignedGrade: string = null
  assignedNote: string = null
  assignGrade(note:string) {
    this.assignedNote = note
    this.stockPositionsService.assignGrade(
      this.currentPosition.positionId,
      this.assignedGrade,
      note).subscribe(
      (_: any) => {
        this.gradingSuccess = "Grade assigned successfully"
      },
      (error) => {
        let errors = GetErrors(error)
        this.gradingError = errors.join(', ')
      }
    );
  }

}
