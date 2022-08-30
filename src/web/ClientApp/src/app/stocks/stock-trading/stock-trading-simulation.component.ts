import { Component, OnInit } from '@angular/core';
import { StocksService, stocktransactioncommand } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { tick } from '@angular/core/testing';

@Component({
  selector: 'stock-trading-simulation',
  templateUrl: './stock-trading-simulation.component.html',
  styleUrls: ['./stock-trading-simulation.component.css']
})

export class StockTradingSimulationComponent implements OnInit {

  stopPrice: number | null = null
  positions: stocktransactioncommand[] = []
  currentCost: number | null = null
  profit: number | null = null
  ticker: string

  constructor(private stocks:StocksService, private route: ActivatedRoute) { }

  ngOnInit(): void {
    var simulations = localStorage.getItem('simulations')
    
    if (simulations) {
      var data = JSON.parse(simulations)

      this.ticker = data.ticker
      this.stopPrice = data.stopPrice
      this.positions = data.positions
      this.currentCost = data.currentCost

      this.updateProfit()
    }
  }

  stockPurchased(stocktransactioncommand: stocktransactioncommand) {

    this.ticker = stocktransactioncommand.ticker
    
    if (stocktransactioncommand.stopPrice) {
      this.stopPrice = stocktransactioncommand.stopPrice
    }

    if (this.currentCost == null) {
      this.currentCost = stocktransactioncommand.price
    }

    this.positions.push(stocktransactioncommand)

    this.update()
  }

  totalCost() {
    return this.positions.reduce((acc, curr) => acc + curr.numberOfShares * curr.price, 0)
  }

  numberOfShares() {
    return this.positions.reduce((acc, curr) => acc + curr.numberOfShares, 0)
  }

  averageCostPerShare() {
    return this.totalCost() / this.numberOfShares()
  }

  riskedAmount() {
    // average cost per share - stopPrice multipled by number of shares
    return (this.averageCostPerShare() - this.stopPrice) * this.numberOfShares()
  }

  removePosition(index:number) {
    this.positions.splice(index, 1)
    this.update()
  }

  reset() {
    localStorage.removeItem('simulations')
    this.positions = []
    this.currentCost = null
    this.profit = null
    this.ticker = null
    this.stopPrice = null
  }

  update() {
    var data = {
      stopPrice: this.stopPrice,
      positions: this.positions,
      currentCost: this.currentCost,
      ticker: this.ticker
    }

    localStorage.setItem('simulations', JSON.stringify(data))

    this.updateProfit()
  }

  updateProfit() {
    this.profit = (this.currentCost - this.averageCostPerShare()) * this.numberOfShares()
  }

}

