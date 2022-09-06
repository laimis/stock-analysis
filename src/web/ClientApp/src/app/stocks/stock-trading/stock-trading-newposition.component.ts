import { DatePipe } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges, Output, EventEmitter, OnInit } from '@angular/core';
import { Prices, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';

@Component({
  selector: 'stock-trading-newposition',
  templateUrl: './stock-trading-newposition.component.html',
  styleUrls: ['./stock-trading-newposition.component.css'],
  providers: [DatePipe]
})
export class StockTradingNewPositionComponent implements OnInit {
  
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

  @Output()
  stockPurchased: EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()

  // variables for new positions
  positionSizeCalculated: number = null
  costToBuy: number | null = null
  numberOfShares : number | null = null
  stopPrice: number | null = null
  oneR: number | null = null
  potentialLoss: number | null = null
  date: string | null = null
  ticker: string | null = null
  notes: string | null = null

  prices: Prices | null = null
  stopAndExitPoints: number[] = []

  ngOnInit(): void {
    this.onBuyTickerSelected("AAPL")
  }

  onBuyTickerSelected(ticker: string) {
    
    this.costToBuy = null
    this.numberOfShares = null
    this.stopPrice = null
    this.ticker = ticker
    this.date = this.datePipe.transform(Date(), 'yyyy-MM-dd');
    
    this.stockService.getStockPrice(ticker)
      .subscribe(price => {
        console.log(price)
        this.costToBuy = price
        this.updateChart(ticker)
      }, error => {
        console.error(error);
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
    this.stockService.getStockPrices2y(ticker).subscribe(
      prices => {
        this.prices = prices
      }
    )
  }

  get20sma(): number {
    return this.getLastSma(0)
  }

  smaCheck20(): boolean {
    return this.get20sma() < this.costToBuy && this.get20sma() > this.get50sma()
  }

  get50sma(): number {
    return this.getLastSma(1)
  }

  smaCheck50(): boolean {
    return this.get50sma() < this.costToBuy && this.get50sma() > this.get150sma()
  }

  get150sma(): number {
    return this.getLastSma(2)
  }

  get200sma(): number {
    return this.getLastSma(3)
  }

  smaCheck150(): boolean {
    return this.get150sma() < this.costToBuy && this.get150sma() < this.get50sma()
  }

  smaCheck200(): boolean {
    return this.get200sma() < this.costToBuy && this.get200sma() < this.get150sma()
  }

  getLastSma(smaIndex): number {
    return this.prices.sma[smaIndex].values.slice(-1)[0]
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
    this.stopAndExitPoints = [this.stopPrice, this.oneR]
  }

  record() {
    console.log("record")
    var cmd = new stocktransactioncommand()
    cmd.ticker = this.ticker
    cmd.numberOfShares = this.numberOfShares
    cmd.price = this.costToBuy
    cmd.stopPrice = this.stopPrice
    cmd.notes = this.notes
    cmd.date = this.date

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
}

