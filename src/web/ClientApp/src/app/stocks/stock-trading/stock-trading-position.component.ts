import { Component, Input } from '@angular/core';
import { BrokerageOrder, PositionEvent, PositionInstance, StocksService, StrategyProfitPoint } from '../../services/stocks.service';
import { Output } from '@angular/core';
import { EventEmitter } from '@angular/core';
import { GetErrors, GetStrategies, toggleVisuallyHidden } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-trading-position',
  templateUrl: './stock-trading-position.component.html',
  styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0
    _position: PositionInstance;

    positionProfitPoints : StrategyProfitPoint[] = []
    positionStrategy: string = null
    positionOrders: BrokerageOrder[] = [];
    allOrders: BrokerageOrder[] = [];
    strategies: { key: string; value: string; }[];

    @Input()
    set position(v:PositionInstance) {
        this._position = v
        this.positionStrategy = v.labels.find(l => l.key == "strategy")?.value
        this.positionProfitPoints = []
        if (this._position) {
            this.setCandidateValues()
            this.updatePositionOrders()
        }
    }

    @Input()
    set orders(value:BrokerageOrder[]) {
        this.allOrders = value;
        this.updatePositionOrders();
    }

    updatePositionOrders() {
        if (!this._position) {
            return
        }

        if (!this.allOrders) {
            return
        }

        this.positionOrders = this.allOrders.filter(o => o.ticker == this._position.ticker)
    }

    @Output()
    positionDeleted = new EventEmitter()

    // constructor that takes stock service
    constructor(
        private stockService:StocksService
    ) {
        this.strategies = GetStrategies()
    }

    fetchProfitPoints() {
        this.stockService.getStrategyProfitPoints(this._position.ticker, this._position.positionId).subscribe(
            (profitPoints) => {
                this.positionProfitPoints = profitPoints
            }
        )
    }
    
    setCandidateValues() {
        this.candidateRiskAmount = this._position.riskedAmount
        this.candidateStopPrice = this._position.stopPrice
    }

    recalculateRiskAmount() {
        var newRiskAmount = (this._position.averageCostPerShare - this.candidateStopPrice) * this._position.numberOfShares
        this.candidateRiskAmount = newRiskAmount
        this._position.riskedAmount = newRiskAmount
        return false
    }

    setStopPrice() {
        this.stockService.setStopPrice(this._position.ticker, this.candidateStopPrice).subscribe(
            (_) => {
                this._position.stopPrice = this.candidateStopPrice
            }
        )
    }

    deleteStopPrice() {
        if (confirm("Are you sure you want to delete the stop price?"))
        {
            this.stockService.deleteStopPrice(this._position.ticker).subscribe(
                (_) => {
                    this._position.stopPrice = null
                    this._position.riskedAmount = null
                }
            )
        }

        return false
    }

    setRiskAmount() {
        if (confirm("Are you sure you want to set the risk amount?")) {
            this.stockService.setRiskAmount(this._position.ticker, this._position.positionId, this.candidateRiskAmount).subscribe(
                (_) => {
                    this._position.riskedAmount = this.candidateRiskAmount
                }
            )
        }
    }

    getCssClassForEvent(e:PositionEvent) {
        return "event-" + e.type
    }

    deletePosition() {
        // prompt user to confirm
        if (confirm("Are you sure you want to delete this position?")) {
            this.stockService.deletePosition(this._position.ticker, this._position.positionId)
                .subscribe(
                (_) => {
                    this._position = null
                    this.positionDeleted.emit()
                })
        }
    }

    
    deleteTransaction(transactionId:string) 
    {
        if (confirm("are you sure you want to delete the transaction?")) {
            this.stockService.deleteStockTransaction(this._position.ticker, transactionId)
                .subscribe(
                    _ => {
                        // refresh UI somehow here, tbd
                    }, (err) => {
                        var errors = GetErrors(err)
                        alert("Error deleting transaction: " + errors.join(", "))
                    })
        }
    }

    clearStrategy() {
        this.stockService.deleteLabel(this._position.ticker, this._position.positionId, "strategy").subscribe(
            (r) => {
            },
            (err) => {
                alert("Error clearing strategy")
            }
        )

        return false
    }

    setStrategy(strategy:string) {
        if (!strategy) {
            alert("Please select strategy")
            return
        }

        let label = {
            key: "strategy",
            value: strategy
        }

        this.stockService.setLabel(this._position.ticker, this._position.positionId, label).subscribe(
            (r) => {
            },
            (err) => {
                alert("Error setting strategy")
            }
        )
    }

    toggleVisibility(elem) {
        toggleVisuallyHidden(elem)
    }
}

