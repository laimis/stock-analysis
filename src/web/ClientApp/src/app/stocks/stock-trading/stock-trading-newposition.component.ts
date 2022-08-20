import { DatePipe } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { brokerageordercommand, Prices, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';

@Component({
  selector: 'stock-trading-newposition',
  templateUrl: './stock-trading-newposition.component.html',
  styleUrls: ['./stock-trading-newposition.component.css'],
  providers: [DatePipe]
})
export class StockTradingNewPositionComponent implements OnChanges {
  
  constructor(
      private stockService:StocksService,
      private datePipe: DatePipe
      )
  { }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.costToBuy != null) {
      console.log("on changes fired")
      this.updateBuyingValues()
    }
  }

  @Input()
  stopLoss: number

  @Input()
  firstTarget: number

  @Input()
  rrTarget: number

  @Output()
  stockPurchased: EventEmitter<string> = new EventEmitter<string>()

  @Output()
  brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()

  // variables for new positions
  positionSize: number = null
  positionSizeCalculated: number = null
  costToBuy: number | null = null
  stocksToBuy : number | null = null
  stopPrice: number | null = null
  notes: string | null = null
  exitPrice: number | null = null
  potentialProfit: number | null = null
  potentialLoss: number | null = null
  potentialRr: number | null = null
  date: string | null = null
  brokerageOrderType: string | null = null
  brokerageOrderDuration: string | null = null

  prices: Prices | null = null
  stopAndExitPoints: number[] | null = null
  ticker: string;

  onBuyTickerSelected(ticker: string) {
      this.costToBuy = null
      this.stocksToBuy = null
      this.stopPrice = null
      this.ticker = ticker
      this.date = this.datePipe.transform(Date(), 'yyyy-MM-dd');
      
      this.stockService.getStockPrice(ticker)
        .subscribe(price => {
          console.log(price)
          this.costToBuy = price
          this.updateBuyingValues()
          this.updateChart(ticker)
        }, error => {
          console.error(error);
        }
      );
    }

  reset() {
    this.costToBuy = null
    this.stocksToBuy = null
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

  get50sma(): number {
    return this.getLastSma(1)
  }

  get150sma(): number {
    return this.getLastSma(2)
  }

  getLastSma(smaIndex): number {
    return this.prices.sma[smaIndex].values.slice(-1)[0]
  }

  updateBuyingValuesWithCostToBuy() {
    console.log("cost to buy: " + this.costToBuy)

    this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
    this.updateBuyingValues()
  }

  updateBuyingValuesWithNumberOfShares() {
    // output to console log stocks to buy and cost to buy values
    console.log("num of shares: stocks to buy: " + this.stocksToBuy + " cost to buy: " + this.costToBuy)

    this.positionSize = this.stocksToBuy * this.costToBuy
    this.updateBuyingValues()
  }

  updateBuyingValuesWithPositionSize() {
    // output to console log stocks to buy and cost to buy values
    console.log("position size: position size: " + this.positionSize + " cost to buy: " + this.costToBuy)

    this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
    this.updateBuyingValues()
  }

  updateBuyingValuesStopPrice() {
    var diff = this.costToBuy - this.stopPrice
    this.potentialLoss = diff * this.stocksToBuy
    this.stopAndExitPoints = [this.stopPrice, this.exitPrice]
  }

  updateBuyingValues() {
    console.log("updateBuyingValues")
    console.log("stocks to buy: " + this.stocksToBuy + " cost to buy: " + this.costToBuy)
    
    if (this.stocksToBuy == null)
    {
      this.positionSize = this.positionSize ? this.positionSize : 3000
      this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
    }
    
    this.positionSizeCalculated = Math.round(this.stocksToBuy * this.costToBuy * 100) / 100

    this.stopPrice = Math.round(this.costToBuy * (1 - this.stopLoss) * 100) / 100
    this.exitPrice = Math.round(this.costToBuy * (1 + this.rrTarget) * 100) / 100
    this.potentialProfit = this.exitPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy
    this.potentialLoss = this.stopPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy 
    this.potentialRr = Math.abs(this.potentialProfit / this.potentialLoss)
    this.stopAndExitPoints = [this.stopPrice, this.exitPrice]
  }

  record() {
    console.log("record")
    var cmd = new stocktransactioncommand()
    cmd.ticker = this.ticker
    cmd.numberOfShares = this.stocksToBuy
    cmd.price = this.costToBuy
    cmd.stopPrice = this.stopPrice
    cmd.notes = this.notes
    cmd.date = this.date

    this.stockService.purchase(cmd).subscribe(
      _ => { 
        this.stockPurchased.emit("purchased")
    },
      _ => { alert('purchase failed') }
    )
  }

  brokerageBuy() {
    var cmd : brokerageordercommand = {
      ticker: this.ticker,
      numberOfShares: this.stocksToBuy,
      price: this.costToBuy,
      type: this.brokerageOrderType,
      duration: this.brokerageOrderDuration
    }

    this.stockService.brokerageBuy(cmd).subscribe(
      _ => { 
        this.reset()
        this.brokerageOrderEntered.emit("entered")
    },
      err => { 
        console.error(err)
        alert('brokerage failed') 
      }
    )
  }
}

