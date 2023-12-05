import {Component, HostListener, Input} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
  BrokerageOrder,
  ChartMarker,
  DailyPositionReport,
  PositionChartInformation,
  PositionInstance,
  PriceFrequency,
  Prices,
  StocksService,
  TradingStrategyResults
} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {green, red} from "../../shared/candlestick-chart/candlestick-chart.component";


@Component({
  selector: 'app-stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent {

  private _index: number = 0
  currentPosition: PositionInstance
  currentPositionPrice: number
  simulationResults: TradingStrategyResults
  simulationErrors: string[];
  scoresErrors: string[];
  pricesErrors: string[]
  dailyPositionReport: DailyPositionReport
  private _positions: PositionInstance[];
  positionChartInformation: PositionChartInformation;

  constructor (
    private stockService: StocksService,
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
  orders:BrokerageOrder[]


  updateCurrentPosition() {
    this.currentPosition = this.positions[this._index]
    // get price data and pass it to chart
    this.gradingError = null
    this.gradingSuccess = null
    this.assignedGrade = this.currentPosition.grade
    this.assignedNote = this.currentPosition.gradeNote
    this.simulationResults = null
    this.dailyPositionReport = null
    this.runTradingStrategies();
    this.positionChartInformation = null
    this.fetchPrice();
    this.setTitle();
  }

  private setTitle() {
    this.title.setTitle(`Trading Review - ${this.currentPosition.ticker} - Nightingale Trading`)
  }

  private fetchPrice() {
    this.stockService.getStockPrice(this.currentPosition.ticker).subscribe(
      (r: number) => {
        this.currentPositionPrice = r;
      }
    );
  }

  private getPositionReport() {
    this.stockService.reportDailyPositionReport(
      this.currentPosition.ticker,
      this.currentPosition.positionId).subscribe(
      (r: DailyPositionReport) => {
        this.scoresErrors = null
        this.dailyPositionReport = r;
        this.getPrices();
      },
      (error) => {
        this.dailyPositionReport = null
        this.scoresErrors = GetErrors(error)
        this.getPrices();
      }
    );
  }

  private runTradingStrategies() {
    this.stockService.simulatePosition(this.currentPosition.ticker, this.currentPosition.positionId).subscribe(
      (r: TradingStrategyResults) => {
        this.simulationErrors = null
        this.simulationResults = r;
        this.getPositionReport()
      },
      (error) => {
        this.simulationErrors = GetErrors(error)
        this.getPositionReport()
      }
    );
  }

  private getPrices() {
    this.stockService.getStockPrices(this.currentPosition.ticker, 365, PriceFrequency.Daily).subscribe(
      (r: Prices) => {

        let markers: ChartMarker[] = []

        this.currentPosition.transactions.filter(t => t.type == 'buy').forEach(t => {
          markers.push({date: t.date, label: 'Buy', color: green, shape: 'arrowUp'})
        })

        this.currentPosition.transactions.filter(t => t.type == 'sell').forEach(t => {
          markers.push({date: t.date, label: 'Sell', color: red, shape: 'arrowDown'})
        })

        this.pricesErrors = null
        this.positionChartInformation = {
          averageBuyPrice: this.currentPosition.averageCostPerShare,
          stopPrice: this.currentPosition.stopPrice,
          markers: markers,
          prices: r,
          ticker: this.currentPosition.ticker
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
    this.stockService.assignGrade(
      this.currentPosition.ticker,
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
