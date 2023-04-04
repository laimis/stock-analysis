import { DatePipe } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Prices, SMA, StockAnalysisOutcome, StockGaps, StocksService, stocktransactioncommand, TickerOutcomes } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-trading-newposition',
  templateUrl: './stock-trading-newposition.component.html',
  styleUrls: ['./stock-trading-newposition.component.css'],
  providers: [DatePipe]
})
export class StockTradingNewPositionComponent {
  
  constructor(
      private stockService:StocksService,
      private datePipe: DatePipe
      )
  { }

  @Input()
  maxLoss: number = 100

  @Input()
  showChart: boolean = true

  @Input()
  recordPositions: boolean = true

  @Input()
  set setTicker(ticker:string) {
    this.onBuyTickerSelected(ticker)
  }

  @Input()
  hideCreatePendingPosition: boolean = false

  @Output()
  stockPurchased: EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()

  @Output()
  pendingPositionCreated: EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()

  // variables for new positions
  positionSizeCalculated: number = null
  costToBuy: number | null = null
  ask: number | null = null
  bid: number | null = null
  numberOfShares : number | null = null
  sizeStopPrice: number | null = null
  positionStopPrice: number | null = null
  oneR: number | null = null
  potentialLoss: number | null = null
  stopPct: number | null = null
  date: string | null = null
  ticker: string | null = null
  notes: string | null = null

  prices: Prices | null = null

  chartTargets: number[] = [];
  chartStops: number[] = [];

  outcomes: TickerOutcomes
  gaps: StockGaps

  onBuyTickerSelected(ticker: string) {
    
    this.costToBuy = null
    this.numberOfShares = null
    this.sizeStopPrice = null
    this.positionStopPrice = null
    this.ticker = ticker
    this.date = this.datePipe.transform(Date(), 'yyyy-MM-dd');
    
    this.stockService.getStockQuote(ticker)
      .subscribe(quote => {
        this.costToBuy = quote.mark
        this.ask = quote.askPrice
        this.bid = quote.bidPrice
        this.updateChart(ticker)
        this.lookupPendingPosition(ticker)
      }, error => {
        console.error(error)
      }
    );

    this.stockService.reportOutcomesAllBars([ticker])
      .subscribe(data => {
        this.outcomes = data.outcomes[0]
        this.gaps = data.gaps[0]
      }, error => {
        console.error(error)
      }
    );
  }

  reset() {
    this.costToBuy = null
    this.numberOfShares = null
    this.positionStopPrice = null
    this.sizeStopPrice = null
    this.ticker = null
    this.prices = null
    this.notes = null
  }

  updateChart(ticker:string) {
    this.stockService.getStockPrices(ticker, 365).subscribe(
      prices => {
        this.prices = prices
      }
    )
  }

  get20sma(): number {
    return this.getLastSma(this.prices.sma.sma20)
  }

  smaCheck20(): boolean {
    return this.get20sma() < this.costToBuy && this.get20sma() > this.get50sma()
  }

  get50sma(): number {
    return this.getLastSma(this.prices.sma.sma50)
  }

  smaCheck50(): boolean {
    return this.get50sma() < this.costToBuy && this.get50sma() > this.get150sma()
  }

  get150sma(): number {
    return this.getLastSma(this.prices.sma.sma150)
  }

  get200sma(): number {
    return this.getLastSma(this.prices.sma.sma200)
  }

  smaCheck150(): boolean {
    return this.get150sma() < this.costToBuy && this.get150sma() < this.get50sma()
  }

  smaCheck200(): boolean {
    return this.get200sma() < this.costToBuy && this.get200sma() < this.get150sma()
  }

  getLastSma(sma:SMA): number {
    return sma.values.slice(-1)[0]
  }

  updateBuyingValuesWithNumberOfShares() {
    if (!this.costToBuy || !this.positionStopPrice) {
      console.log("not enough info to calculate")
      return
    }
    var singleShareLoss = this.costToBuy - this.positionStopPrice
    this.updateBuyingValues(singleShareLoss)
  }

  updateBuyingValuesSizeStopPrice() {
    if (!this.costToBuy) {
      console.log("sizeStopChanged: not enough info to calculate the rest")
      return
    }

    if (!this.sizeStopPrice) {
      console.log("sizeStopChanged: no size stop price")
      return
    }

    var singleShareLoss = this.costToBuy - this.sizeStopPrice
    this.numberOfShares = Math.floor(this.maxLoss / singleShareLoss)
    
    this.updateBuyingValues(singleShareLoss)
  }

  updateBuyingValuesPositionStopPrice() {
    if (!this.costToBuy) {
      console.log("positionStopChanged: not enough info to calculate the rest")
      return
    }
    var singleShareLoss = this.costToBuy - this.positionStopPrice
    this.updateBuyingValues(singleShareLoss)
  }

  updateBuyingValuesWithCostToBuy() {
    this.updateBuyingValuesSizeStopPrice()
  }

  updateBuyingValues(singleShareLoss:number) {
    console.log("updateBuyingValues")
    // how many shares can we buy to keep the loss under $100
    let positionStopPrice = this.positionStopPrice
    if (!positionStopPrice) {
      positionStopPrice = this.sizeStopPrice
    }

    this.positionSizeCalculated = Math.round(this.numberOfShares * this.costToBuy * 100) / 100
    this.oneR = this.costToBuy + singleShareLoss
    this.potentialLoss = positionStopPrice * this.numberOfShares - this.costToBuy * this.numberOfShares
    this.stopPct = Math.round((positionStopPrice - this.costToBuy) / this.costToBuy * 100) / 100
    this.chartTargets = [this.oneR]
    this.chartStops = [positionStopPrice]
  }

  record() {
    console.log("record")
    var cmd = this.createPurchaseCommand();

    if (!this.recordPositions) {
      this.stockPurchased.emit(cmd)
      return
    }

    this.stockService.purchase(cmd).subscribe(
      _ => { 
        this.stockPurchased.emit(cmd)
    },
      _ => { alert('purchase failed') }
    )
  }

  private createPurchaseCommand() {
    var cmd = new stocktransactioncommand();
    cmd.ticker = this.ticker;
    cmd.numberOfShares = this.numberOfShares;
    cmd.price = this.costToBuy;
    cmd.stopPrice = this.positionStopPrice;
    cmd.notes = this.notes;
    cmd.date = this.date;
    return cmd;
  }

  createPendingPosition() {
    var cmd = this.createPurchaseCommand()

    this.stockService.createPendingStockPosition(cmd).subscribe(
      _ => {
        this.ticker = null
        this.prices = null
        this.gaps = null
        this.outcomes = null
        this.pendingPositionCreated.emit(cmd)
        this.reset()
      },
      errors => { 
        var errorMessageArray = GetErrors(errors)
        var errorMessages = errorMessageArray.join(", ")
        alert('pending position failed: ' + errorMessages) 
      }
    )
  }

  filteredOutcomes(): StockAnalysisOutcome[] {
    if (!this.outcomes) {
      return []
    }
    return this.outcomes.outcomes.filter(o => o.type !== "Neutral")
  }

  lookupPendingPosition(ticker: string) {
    this.stockService.getPendingStockPositions().subscribe(
      positions => {
        var position = positions.find(p => p.ticker === ticker)
        if (position) {
          this.costToBuy = position.price
          this.numberOfShares = position.numberOfShares
          this.positionStopPrice = position.stopPrice
          this.notes = position.notes
          this.updateBuyingValuesPositionStopPrice()
        }
      }
    )
  }
}

