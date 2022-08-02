import { Component, Input, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { Prices, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';

@Component({
  selector: 'stock-trading-newposition',
  templateUrl: './stock-trading-newposition.component.html',
  styleUrls: ['./stock-trading-newposition.component.css']
})
export class StockTradingNewPositionComponent implements OnChanges {
  
  constructor(
      private stockService:StocksService
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

  // variables for new positions
  positionSize: number = null
  positionSizeCalculated: number = null
  costToBuy: number | null = null
  stocksToBuy : number | null = null
  stopPrice: number | null = null
  exitPrice: number | null = null
  potentialProfit: number | null = null
  potentialLoss: number | null = null
  potentialRr: number | null = null

  prices: Prices | null = null
  stopAndExitPoints: number[] | null = null
  ticker: string;

  onBuyTickerSelected(ticker: string) {
      this.costToBuy = null
      this.stocksToBuy = null
      this.stopPrice = null
      this.ticker = ticker
      
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
    // first, we calculate the absolute amount that could be lost if we used the position size and the stop loss % that's specified
    var potentialLoss = this.positionSize * this.stopLoss

    var diff = this.costToBuy - this.stopPrice

    console.log("diff: " + diff + " potential loss: " + potentialLoss)

    // now we take the cost to buy and figure out how many shares can we buy that will keep us from losing the potential loss
    this.stocksToBuy = Math.floor(potentialLoss / diff)
    this.positionSize = this.stocksToBuy * this.costToBuy 
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

  buy() {
    console.log("buy")
    var cmd = new stocktransactioncommand()
    cmd.ticker = this.ticker
    cmd.numberOfShares = this.stocksToBuy
    cmd.price = this.costToBuy
    cmd.date = new Date().toISOString()

    this.stockService.purchase(cmd).subscribe(
      _ => { 
        this.stockPurchased.emit("purchased")
    },
      _ => { alert('purchase failed') }
    )
  }
}

