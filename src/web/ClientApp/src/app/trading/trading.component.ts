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
  singleR: number = 0.07

  updateModel() {
    this.numberOfPositions = this.result.length
    
    this.invested = 0
    this.result.forEach(e => {
      this.invested += e.averageCost * e.owned
    })
  }

  periodChanged() {
    this.loadEntries()
  }
}

