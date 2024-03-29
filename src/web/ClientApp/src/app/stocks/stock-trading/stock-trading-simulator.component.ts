import {Component, OnInit} from '@angular/core';
import {openpositioncommand, PositionInstance} from '../../services/stocks.service';
import {StockPositionsService} from "../../services/stockpositions.service";

class StockTransaction {
    numberOfShares: number
    price: number
    date: string
    type: string
}

@Component({
    selector: 'app-stock-trading-simulation',
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
    cost: number = 0
    profit: number = 0
    averageCostPerShare: number = 0
    numberOfShares: number = 0
    riskedAmount: number = 0
    unrealizedProfit: number = 0
    unrealizedGain: number = 0
    unrealizedRR: number = 0
    averageSaleCostPerShare: number = 0
    averageBuyCostPerShare: number = 0
    r1: number = 0
    r2: number = 0
    r4: number = 0
    rrs: string[] = []

    showExisting: boolean = false
    positionFilter: string = ''
    positions: PositionInstance[] = []
    filteredPositions: PositionInstance[] = []

    constructor(private stocks: StockPositionsService) {
    }

    ngOnInit(): void {
        var simulations = localStorage.getItem('simulations')
        if (simulations) {
            var data = JSON.parse(simulations)

            this.ticker = data.ticker
            this.stopPrice = data.stopPrice
            this.transactions = data.transactions
            this.currentCost = data.currentCost
            this.riskedAmount = data.riskedAmount

            this.runCalculations()
        }
    }

    initialPosition(ticker: string, cmd: openpositioncommand) {
        this.ticker = ticker
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

    addTransaction(type: string) {

        if (this.price && this.quantity) {
            var transaction = {
                numberOfShares: this.quantity,
                price: this.price,
                date: this.date,
                type: type
            }

            this.currentCost = transaction.price

            this.transactions.push(transaction)

            this.update()
        }
    }

    removeTransaction(index: number) {
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
            transactions: this.transactions,
            currentCost: this.currentCost,
            ticker: this.ticker,
            riskedAmount: this.riskedAmount
        }

        localStorage.setItem('simulations', JSON.stringify(data))

        this.runCalculations()
    }

    calculateRiskedAmount() {
        var r = this.averageCostPerShare - this.stopPrice
        this.riskedAmount = Math.round(r * this.numberOfShares * 100) / 100

        this.updateRiskParameters()

        this.update()
    }

    updateRiskParameters() {
        var r = this.averageCostPerShare - this.stopPrice
        this.r1 = this.averageCostPerShare + r
        this.r2 = this.averageCostPerShare + r * 2
        this.r4 = this.averageCostPerShare + r * 4

        this.rrs = []
        for (let i = 1; i < 10; i++) {
            this.rrs.push(i + "R: " + (this.averageCostPerShare + r * i))
        }
    }

    showExistingPositions() {
        this.stocks.getTradingEntries().subscribe(entries => {
            this.positions = entries.current;
            this.filteredPositions = entries.current;
            this.showExisting = true
        })
    }

    filterPositions() {
        this.filteredPositions = this.positions.filter(p => p.ticker.toLowerCase().indexOf(this.positionFilter.toLowerCase()) > -1)
    }

    loadPosition(p: PositionInstance) {

        this.reset()
        var first = true;
        this.riskedAmount = p.riskedAmount
        p.transactions.forEach(t => {
            if (first) {
                var cmd: openpositioncommand = {
                    stopPrice: p.stopPrice,
                    price: t.price,
                    numberOfShares: t.numberOfShares,
                    date: t.date,
                    notes: null,
                    ticker: p.ticker,
                    strategy: null,
                }
                this.initialPosition(p.ticker, cmd)
                first = false
            } else {
                this.price = t.price
                this.quantity = t.numberOfShares
                this.date = t.date
                this.addTransaction(t.type)
            }
        })
        this.updateRiskParameters()
        this.showExisting = false
    }

    runCalculations() {
        var slots: number[] = []
        var cost: number = 0
        var profit: number = 0
        var numberOfShares: number = 0

        var totalSale: number = 0
        var totalNumberOfSharesSold = 0

        var totalBuy: number = 0
        var totalNumberOfSharesBought = 0

        this.transactions.forEach(transaction => {
            if (transaction.type == 'buy') {
                for (let i = 0; i < transaction.numberOfShares; i++) {
                    slots.push(transaction.price)
                    cost += transaction.price
                    numberOfShares++
                }

                totalBuy += transaction.price * transaction.numberOfShares
                totalNumberOfSharesBought += transaction.numberOfShares
            } else {
                // remove quantity number of slots from the beginning of an array
                var removed = slots.splice(0, transaction.numberOfShares)
                removed.forEach(removedElement => {
                    profit += transaction.price - removedElement
                    cost -= removedElement
                    numberOfShares--
                })

                totalSale += transaction.price * transaction.numberOfShares
                totalNumberOfSharesSold += transaction.numberOfShares
            }
        });

        // calculate average cost per share using slots
        this.averageCostPerShare = slots.reduce((acc, curr) => acc + curr, 0) / slots.length
        this.cost = cost
        this.profit = profit
        this.numberOfShares = numberOfShares

        this.averageSaleCostPerShare = totalSale / totalNumberOfSharesSold
        this.averageBuyCostPerShare = totalBuy / totalNumberOfSharesBought

        // unrealized profit is profit that is realized + unrealized
        if (this.numberOfShares > 0) {
            this.unrealizedProfit = slots.reduce((acc, curr) => acc + this.currentCost - curr, 0)
            this.unrealizedGain = this.unrealizedProfit / this.cost
            this.unrealizedRR = this.unrealizedProfit / this.riskedAmount
        } else {
            this.unrealizedProfit = 0
            this.unrealizedGain = 0
            this.unrealizedRR = 0
        }
    }

}

