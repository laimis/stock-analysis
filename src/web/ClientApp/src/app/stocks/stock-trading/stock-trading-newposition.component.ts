import { DatePipe } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Prices, SMA, StockAnalysisOutcome, StockGaps, StocksService, stocktransactioncommand, TickerOutcomes } from 'src/app/services/stocks.service';

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

  @Output()
  stockPurchased: EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()

  // variables for new positions
  positionSizeCalculated: number = null
  costToBuy: number | null = null
  ask: number | null = null
  bid: number | null = null
  numberOfShares : number | null = null
  stopPrice: number | null = null
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
    this.stopPrice = null
    this.ticker = ticker
    this.date = this.datePipe.transform(Date(), 'yyyy-MM-dd');
    
    this.stockService.getStockQuote(ticker)
      .subscribe(quote => {
        this.costToBuy = quote.mark
        this.ask = quote.askPrice
        this.bid = quote.bidPrice
        this.updateChart(ticker)
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
    this.stopPrice = null
    this.ticker = null
    this.prices = null
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
    if (!this.costToBuy || !this.stopPrice) {
      console.log("not enough info to calculate")
      return
    }
    var singleShareLoss = this.costToBuy - this.stopPrice
    this.updateBuyingValues(singleShareLoss)
  }

  updateBuyingValuesStopPrice() {
    this.updateBuyingValuesPricesChanged()
  }

  updateBuyingValuesWithCostToBuy() {
    this.updateBuyingValuesPricesChanged()
  }

  updateBuyingValuesPricesChanged() {
    if (!this.costToBuy || !this.stopPrice) {
      console.log("not enough info to calculate")
      return
    }

    var singleShareLoss = this.costToBuy - this.stopPrice
    this.numberOfShares = Math.floor(this.maxLoss / singleShareLoss)
    
    this.updateBuyingValues(singleShareLoss)
  }

  updateBuyingValues(singleShareLoss:number) {
    console.log("updateBuyingValues")
    // how many shares can we buy to keep the loss under $100
    this.positionSizeCalculated = Math.round(this.numberOfShares * this.costToBuy * 100) / 100
    this.oneR = this.costToBuy + singleShareLoss
    this.potentialLoss = this.stopPrice * this.numberOfShares - this.costToBuy * this.numberOfShares
    this.stopPct = Math.round((this.stopPrice - this.costToBuy) / this.costToBuy * 100) / 100
    this.chartTargets = [this.oneR]
    this.chartStops = [this.stopPrice]
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
    cmd.stopPrice = this.stopPrice;
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
        this.stockPurchased.emit(cmd)
      },
      _ => { alert('purchase failed') }
    )
  }

  filteredOutcomes(): StockAnalysisOutcome[] {
    if (!this.outcomes) {
      return []
    }
    return this.outcomes.outcomes.filter(o => o.type !== "Neutral")
  }
}

