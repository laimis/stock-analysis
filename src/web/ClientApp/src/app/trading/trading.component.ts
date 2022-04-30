import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { StocksService, ReviewList, StockTradingGridEntry, stocktransactioncommand } from '../services/stocks.service';

@Component({
  selector: 'app-trading',
  templateUrl: './trading.component.html',
  styleUrls: ['./trading.component.css']
})
export class TradingComponent implements OnInit {
  result: StockTradingGridEntry[]
  loaded: boolean = false
  timePeriod: string = 'thisweek'

  constructor(
    private stockService:StocksService,
    private title: Title
    ) { }

  ngOnInit() {
    this.title.setTitle("Review - Nightingale Trading")
    this.loadEntries()
  }

  private loadEntries() {
    this.stockService.getTradingEntries().subscribe((r: StockTradingGridEntry[]) => {
      this.result = r
      this.updateModel()
      this.loaded = true
      
    }, _ => { this.loaded = true})
  }

  numberOfPositions: number = 0
  invested: number = 0
  rrTarget: number = 0.15 // this is const, not sure yet where we will keep
  firstTarget: number = 0.07
  stopLoss: number = 0.05

  // variables for new positions
  positionSize: number = 3000
  costToBuy: number | null = null
  stocksToBuy : number | null = null
  stopPrice: number | null = null
  exitPrice: number | null = null
  potentialGains: number | null = null
  potentialLoss: number | null = null
  potentialRr: number | null = null
  stats: object | null = null

  onBuyTickerSelected(ticker: string) {
    this.costToBuy = null

    this.stockService.getStockDetails(ticker).subscribe(stockDetails => {
      console.log(stockDetails)
      this.costToBuy = stockDetails.price
      this.stocksToBuy = Math.floor(this.positionSize / this.costToBuy)
      this.stopPrice = this.costToBuy * (1 - this.stopLoss)
      this.exitPrice = this.costToBuy * (1 + this.rrTarget)
      this.potentialGains = this.exitPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy
      this.potentialLoss = this.stopPrice * this.stocksToBuy - this.costToBuy * this.stocksToBuy 
      this.potentialRr = Math.abs(this.potentialGains / this.potentialLoss)
      this.stats = stockDetails.stats
		}, error => {
			console.error(error);
			this.loaded = true;
    });
  }

  updateModel() {
    this.numberOfPositions = this.result.length
    
    this.invested = 0
    this.result.forEach(e => {
      this.invested += e.averageCost * e.numberOfShares
    })
  }

  periodChanged() {
    this.loadEntries()
  }
}

