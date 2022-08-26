import { Component } from '@angular/core';
import { StocksService, stocktransactioncommand } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'stock-trading-simulation',
  templateUrl: './stock-trading-simulation.component.html',
  styleUrls: ['./stock-trading-simulation.component.css']
})

export class StockTradingSimulationComponent {

  stopPrice: number | null = null
  positions: stocktransactioncommand[] = []
  currentCost: number | null = null
  profit: number | null = null
  constructor(private stocks:StocksService, private route: ActivatedRoute) { }


  stockPurchased(stocktransactioncommand: stocktransactioncommand) {
    console.log(stocktransactioncommand)
    if (stocktransactioncommand.stopPrice) {
      this.stopPrice = stocktransactioncommand.stopPrice
    }

    if (this.currentCost == null) {
      this.currentCost = stocktransactioncommand.price
    }

    this.positions.push(stocktransactioncommand)
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

  updateProfit() {
    this.profit = (this.currentCost - this.averageCostPerShare()) * this.numberOfShares()
  }

}

