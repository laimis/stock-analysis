import { Component, OnInit } from '@angular/core';
import { StocksService, stocktransactioncommand, StockTradingPosition } from '../../services/stocks.service';

class StockTransaction {
  numberOfShares: number
  price: number
  date: string
  type: string
}

@Component({
  selector: 'stock-trading-simulation',
  templateUrl: './stock-trading-simulator.component.html',
  styleUrls: ['./stock-trading-simulator.component.css']
})

export class StockTradingSimulatorComponent implements OnInit {

  stopPrice: number | null = null
  transactions: StockTransaction[] = []
  currentCost: number | null = null
  ticker: string

  price: number | null = null
  quantity: number | null = null
  date: string

  // dynamic calculated fields
  cost : number = 0
  profit: number = 0
  averageCostPerShare: number = 0
  numberOfShares: number = 0
  riskedAmount: number = 0
  unrealizedProfit: number = 0


  showExisting: boolean = false

  constructor(private stocks:StocksService) { }

  ngOnInit(): void {
    var simulations = localStorage.getItem('simulations')
    if (simulations) {
      var data = JSON.parse(simulations)

      this.ticker = data.ticker
      this.stopPrice = data.stopPrice
      this.transactions = data.positions
      this.currentCost = data.currentCost
      this.riskedAmount = data.riskedAmount

      this.runCalculations()
    }
  }

  initialPosition(cmd:stocktransactioncommand) {
    this.ticker = cmd.ticker
    this.stopPrice = cmd.stopPrice
    this.price = cmd.price
    this.quantity = cmd.numberOfShares
    this.date = cmd.date
    this.addTransaction('buy')
  }

  buy() {
    this.addTransaction('buy')
  }
  sell() {
    this.addTransaction('sell')
  }

  addTransaction(type:string) {

    if (this.price && this.quantity) {
      var transaction = {
        numberOfShares: this.quantity,
        price: this.price,
        date: this.date,
        type: type
      }

      if (this.currentCost == null) {
        this.currentCost = transaction.price
      }

      this.transactions.push(transaction)

      this.update()
    }
  }


  removeTransaction(index:number) {
    this.transactions.splice(index, 1)
    this.update()
  }

  reset() {
    localStorage.removeItem('simulations')
    this.transactions = []
    this.currentCost = null
    this.profit = null
    this.ticker = null
    this.stopPrice = null
    this.riskedAmount = null
  }

  update() {
    
    var data = {
      stopPrice: this.stopPrice,
      positions: this.transactions,
      currentCost: this.currentCost,
      ticker: this.ticker,
      riskedAmount: this.riskedAmount
    }

    localStorage.setItem('simulations', JSON.stringify(data))

    this.runCalculations()
  }

  calculateRiskedAmount() {
    this.riskedAmount = Math.round((this.averageCostPerShare - this.stopPrice) * this.numberOfShares * 100) / 100
    this.update()
  }

  positionFilter:string = ''
  positions:StockTradingPosition[] = []
  filteredPositions:StockTradingPosition[] = []

  showExistingPositions() {
    this.stocks.getStockPositions().subscribe(positions => {
      this.positions = positions
      this.filteredPositions = positions
      this.showExisting = true
    })
  }

  filterPositions() {
    this.filteredPositions = this.positions.filter(p => p.ticker.toLowerCase().indexOf(this.positionFilter.toLowerCase()) > -1)
  }

  loadPosition(p:StockTradingPosition) {
    
    this.reset()
    var tx = p.buys.concat(p.sells).sort((a, b) => Date.parse(a.when) - Date.parse(b.when))
    var first = true;
    this.currentCost = p.price
    tx.forEach(t => {
      if (first) {
        var cmd:stocktransactioncommand = {
          ticker: p.ticker,
          stopPrice: p.stopPrice,
          price: t.price,
          numberOfShares: t.quantity,
          date: t.when,
          notes: null
        }
        this.initialPosition(cmd)
        first = false
      }
      else {
        this.price = t.price
        this.quantity = t.quantity
        this.date = t.when
        this.addTransaction(t.type)
      }
    })
    this.showExisting = false
  }

  runCalculations() {
    var slots : number[] = []
    var cost : number = 0
    var profit : number = 0
    var numberOfShares : number = 0

    this.transactions.forEach(transaction => {
      if (transaction.type == 'buy') {
        for (let i = 0; i < transaction.numberOfShares; i++) {
          slots.push(transaction.price)
          cost += transaction.price
          numberOfShares++
        }
      } else {
        // remove quantity number of slots from the beginning of an array
        var removed = slots.splice(0, transaction.numberOfShares)
        removed.forEach(removedElement => {
          profit += transaction.price - removedElement
          cost -= removedElement
          numberOfShares--
        })
      }
    });

    // calculate average cost per share using slots
    this.averageCostPerShare = slots.reduce((acc, curr) => acc + curr, 0) / slots.length
    this.cost = cost
    this.profit = profit
    this.numberOfShares = numberOfShares
    
    // unrealized profit is profit that is realized + unrealized
    this.unrealizedProfit = slots.reduce((acc, curr) => acc + this.currentCost - curr, 0) + profit
  }

}

