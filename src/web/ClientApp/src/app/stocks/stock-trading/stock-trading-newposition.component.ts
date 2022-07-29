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
  positionSize: number = 3000
  costToBuy: number | null = null
  stocksToBuy : number | null = null
  stopPrice: number | null = null
  exitPrice: number | null = null
  potentialGains: number | null = null
  potentialLoss: number | null = null
  potentialRr: number | null = null

  prices: Prices | null = null
  stopAndExitPoints: number[] | null = null
  ticker: string;

  onBuyTickerSelected(ticker: string) {
      this.costToBuy = null
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

  updateBuyingValues() {
    console.log("updateBuyingValues")
    this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
    this.stopPrice = Math.round(this.costToBuy * (1 - this.stopLoss) * 100) / 100
    this.exitPrice = Math.round(this.costToBuy * (1 + this.rrTarget) * 100) / 100
    this.potentialGains = this.exitPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy
    this.potentialLoss = this.stopPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy 
    this.potentialRr = Math.abs(this.potentialGains / this.potentialLoss)
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

