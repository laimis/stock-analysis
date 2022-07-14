import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { StocksService, StockTradingGridEntry, StockStats } from '../../services/stocks.service';

@Component({
  selector: 'stock-trading',
  templateUrl: './stock-trading-dashboard.component.html',
  styleUrls: ['./stock-trading-dashboard.component.css']
})
export class StockTradingComponent implements OnInit {
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

  activeTab = 'positions'
  
  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
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
