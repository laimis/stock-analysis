import {Component, HostListener, Input} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
  DailyPositionReport,
  PositionInstance, PositionChartInformation,
  Prices,
  StocksService,
  TradingStrategyResults
} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';


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

  updateCurrentPosition() {
    this.currentPosition = this.positions[this._index]
    // get price data and pass it to chart
    this.gradingError = null
    this.gradingSuccess = null
    this.assignedGrade = this.currentPosition.grade
    this.assignedNote = this.currentPosition.gradeNote

    this.runTradingStrategies();
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
    this.stockService.getStockPrices(this.currentPosition.ticker, 365).subscribe(
      (r: Prices) => {
        this.positionChartInformation = {
          averageBuyPrice: this.currentPosition.averageCostPerShare,
          stopPrice: this.currentPosition.stopPrice,
          buyDates: this.currentPosition.transactions.filter(t => t.type == 'buy').map(t => t.date),
          sellDates: this.currentPosition.transactions.filter(t => t.type == 'sell').map(t => t.date),
          prices: r,
          ticker: this.currentPosition.ticker
        }
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
