import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Prices, StocksService, StockStats } from 'src/app/services/stocks.service';

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

  // variables for new positions
  positionSize: number = 3000
  costToBuy: number | null = null
  stocksToBuy : number | null = null
  stopPrice: number | null = null
  exitPrice: number | null = null
  potentialGains: number | null = null
  potentialLoss: number | null = null
  potentialRr: number | null = null
  stats: StockStats | null = null

  prices: Prices | null = null
  stopAndExitPoints: number[] | null = null

  onBuyTickerSelected(ticker: string) {
      this.costToBuy = null
  
      this.stockService.getStockDetails(ticker)
        .subscribe(stockDetails => {
          console.log(stockDetails)
          this.costToBuy = stockDetails.price
          this.stats = stockDetails.stats
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
    this.stopPrice = this.costToBuy * (1 - this.stopLoss)
    this.exitPrice = this.costToBuy * (1 + this.rrTarget)
    this.potentialGains = this.exitPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy
    this.potentialLoss = this.stopPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy 
    this.potentialRr = Math.abs(this.potentialGains / this.potentialLoss)
    this.stopAndExitPoints = [this.stopPrice, this.exitPrice]
  }
}

