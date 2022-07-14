import { Component, Input, OnInit } from '@angular/core';
import { StocksService, StockStats } from 'src/app/services/stocks.service';

@Component({
  selector: 'stock-trading-newposition',
  templateUrl: './stock-trading-newposition.component.html',
  styleUrls: ['./stock-trading-newposition.component.css']
})
export class StockTradingNewPositionComponent {

    constructor(
        private stockService:StocksService
        ) { }

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

    onBuyTickerSelected(ticker: string) {
        this.costToBuy = null
    
        this.stockService.getStockDetails(ticker).subscribe(stockDetails => {
          console.log(stockDetails)
          this.costToBuy = stockDetails.price
          this.updateBuyingValues()
          this.stats = stockDetails.stats
            }, error => {
                console.error(error);
            }
        );
      }

    updateBuyingValues() {
        this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
        this.stopPrice = this.costToBuy * (1 - this.stopLoss)
        this.exitPrice = this.costToBuy * (1 + this.rrTarget)
        this.potentialGains = this.exitPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy
        this.potentialLoss = this.stopPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy 
        this.potentialRr = Math.abs(this.potentialGains / this.potentialLoss)
      }
}

