import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {
  DailyPositionReport,
  PastStockTradingPositions,
  PositionInstance,
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
export class StockTradingReviewComponent implements OnInit {

  positions: PositionInstance[]
  private _index: number = 0
  currentPosition: PositionInstance
  currentPositionPrice: number
  simulationResults: TradingStrategyResults
  prices: Prices
  positionBuys: string[]
  positionSells: string[]
  simulationErrors: string[];
  scoresErrors: string[];
  dailyPositionReport: DailyPositionReport

  constructor (
    private stockService: StocksService,
    private title: Title) { }

  ngOnInit(): void {
    this.stockService.getPastTradingEntries().subscribe((r: PastStockTradingPositions) => {
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
    this.gradingError = null
    this.gradingSuccess = null
    this.assignedGrade = this.currentPosition.grade
    this.assignedNote = this.currentPosition.gradeNote
    this.positionBuys = this.currentPosition.transactions.filter(t => t.type == 'buy').map(t => t.date)
    this.positionSells = this.currentPosition.transactions.filter(t => t.type == 'sell').map(t => t.date)

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
        this.prices = r;
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
