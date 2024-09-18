import {Component, EventEmitter, Input, Output} from '@angular/core';
import {
    BrokerageOrder,
    PositionEvent,
    PositionInstance,
    StockQuote,
    StrategyProfitPoint
} from '../../services/stocks.service';
import {GetErrors, GetStrategies, toggleVisuallyHidden} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {FormControl} from "@angular/forms";

@Component({
    selector: 'app-stock-trading-position',
    templateUrl: './stock-trading-position.component.html',
    styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0
    numberOfProfitPoints: number = 4
    positionProfitPoints: StrategyProfitPoint[] = []
    positionStrategy: string = null
    positionOrders: BrokerageOrder[] = [];
    allOrders: BrokerageOrder[] = [];
    strategies: { key: string; value: string; }[];
    showOrderForm: boolean = false;
    
    @Input()
    quote: StockQuote
    @Output()
    positionDeleted = new EventEmitter()
    @Output()
    brokerageOrdersChanged = new EventEmitter<string>()
    @Output()
    notesChanged = new EventEmitter<string>()

    // constructor that takes stock service
    constructor(
        private stockService: StockPositionsService
    ) {
        this.strategies = GetStrategies()
    }

    _position: PositionInstance;

    @Input()
    set position(v: PositionInstance) {
        this._position = v

        if (this._position) {
            this.positionStrategy = v.labels.find(l => l.key == "strategy")?.value
            this.positionProfitPoints = []
            this.setCandidateValues()
            this.updatePositionOrders()
        }
    }

    @Input()
    set orders(value: BrokerageOrder[]) {
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

    fetchProfitPoints() {
        this.stockService.getStrategyProfitPoints(
            this._position.positionId,
            this.numberOfProfitPoints).subscribe(
            (profitPoints) => {
                this.positionProfitPoints = profitPoints
            }
        )
    }

    profitPointReached(price: number) {
        if (!this.quote) {
            return false
        }
        
        if (this._position.isShort) {
            return price >= this.quote.price
        }

        return price <= this.quote.price
    }

    setCandidateValues() {
        // round risked amount to 2 decimal places
        this.candidateRiskAmount = Math.round(this._position.riskedAmount * 100) / 100
        this.candidateStopPrice = Math.round(this._position.stopPrice * 100) / 100
    }

    recalculateRiskAmount() {
        const newRiskAmount = (this._position.averageCostPerShare - this.candidateStopPrice) * this._position.numberOfShares;
        this.candidateRiskAmount = newRiskAmount
        this._position.riskedAmount = newRiskAmount
        return false
    }

    deleteStopPrice() {
        if (confirm("Are you sure you want to delete the stop price?")) {
            this.stockService.deleteStopPrice(this._position.positionId).subscribe(
                (_) => {
                    this._position.stopPrice = null
                    this._position.riskedAmount = null
                }
            )
        }

        return false
    }

    setRiskAmount(elementVisibilityToToggle: HTMLElement[]) {
        if (confirm("Are you sure you want to set the risk amount?")) {
            this.stockService.setRiskAmount(this._position.positionId, this.candidateRiskAmount).subscribe(
                (_) => {
                    this._position.riskedAmount = this.candidateRiskAmount
                    elementVisibilityToToggle.forEach(this.toggleVisibility)
                }
            )
        }
    }

    getCssClassForEvent(e: PositionEvent) {
        return "event-" + e.type
    }

    deletePosition() {
        // prompt user to confirm
        if (confirm("Are you sure you want to delete this position?")) {
            this.stockService.deletePosition(this._position.positionId)
                .subscribe(
                    (_) => {
                        this._position = null
                        this.positionDeleted.emit()
                    },
                    err => {
                        let errors = GetErrors(err)
                        alert("Error deleting position: " + errors.join(", "))
                    })
        }
    }

    closePosition(closeReason:string) {
        this.stockService.closePosition(this._position.positionId, closeReason)
            .subscribe(
                (_) => {
                    this.brokerageOrdersChanged.emit()
                },
                err => {
                    let errors = GetErrors(err)
                    alert("Error closing position: " + errors.join(", "))
                })
    }

    showCloseModal: boolean = false;
    closeReason: string = '';
    openCloseModal() {
        this.showCloseModal = true;
    }
    closeCloseModal() {
        this.showCloseModal = false;
        this.closeReason = '';
    }
    confirmClosePosition() {
        // Call the existing closePosition() method
        this.closePosition(this.closeReason);

        // Close the modal
        this.closeCloseModal();
    }
    
    showStopModal: boolean = false;
    stopReason: string = '';
    stopErrors = [];
    openStopModal() {
        this.showStopModal = true;
    }
    closeStopModal() {
        this.showStopModal = false;
        this.stopReason = '';
    }
    confirmStop(stopPriceValue: string) {
        this.stopErrors = []
        // Parse the stop price value
        let stopPrice = parseFloat(stopPriceValue)
        if (isNaN(stopPrice)) {
            this.stopErrors = ["Please provide a valid stop"]
            return
        }
        
        // check that stop reason is not empty
        if (!this.stopReason) {
            this.stopErrors = ["Please provide a reason for the stop"]
            return
        }
        
        this.stockService.setStopPrice(this._position.positionId, stopPrice, this.stopReason).subscribe(
            (_) => {
                this.candidateStopPrice = stopPrice
                this._position.stopPrice = stopPrice
                this.closeStopModal()
            },
            err => {
                let errors = GetErrors(err)
                alert("Error setting stop price: " + errors.join(", "))
                this.closeStopModal()
            }
        )
        
        // Close the modal
        this.closeStopModal();
    }

    showNotesForm: boolean = false;
    notesControl = new FormControl('')
    addNotes() {
        if (this.notesControl.invalid === false) {
            this.stockService.addNotes(this._position.positionId, this.notesControl.value).subscribe(
                (_) => {
                    this.notesControl.setValue('')
                    this.showNotesForm = false
                    this.notesChanged.emit()
                },
                err => {
                    let errors = GetErrors(err)
                    alert("Error adding notes: " + errors.join(", "))
                }
            )
        }
    }

    deleteTransaction(transactionId: string) {
        if (confirm("are you sure you want to delete the transaction?")) {
            this.stockService.deleteTransaction(this._position.positionId, transactionId)
                .subscribe(
                    _ => {
                        // refresh UI somehow here, tbd
                    }, (err) => {
                        var errors = GetErrors(err)
                        alert("Error deleting transaction: " + errors.join(", "))
                    })
        }
    }

    clearStrategy(elementVisibilityToToggle: HTMLElement[]) {
        if (!confirm("Are you sure you want to clear the strategy?")) {
            return false
        }

        this.stockService.deleteLabel(this._position.positionId, "strategy").subscribe(
            _ => {
                this.positionStrategy = null
                elementVisibilityToToggle.forEach(this.toggleVisibility)
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error clearing strategy: " + errors.join(", "))
            }
        )

        return false
    }

    setStrategy(strategy: string, elementVisibilityToToggle: HTMLElement[]) {
        if (!strategy) {
            alert("Please select strategy")
            return
        }

        let label = {
            key: "strategy",
            value: strategy
        }

        this.stockService.setLabel(this._position.positionId, label).subscribe(
            _ => {
                this.positionStrategy = strategy
                elementVisibilityToToggle.forEach(this.toggleVisibility)
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error setting strategy: " + errors.join(", "))
            }
        )
    }

    toggleVisibility(elem) {
        toggleVisuallyHidden(elem)
    }

    getPrice() {
        if (!this.quote) {
            return null
        }

        return this.quote.price
    }

    getUnrealizedProfit() {
        if (!this.quote) {
            return null
        }

        return this._position.profit + (this.quote.price - this._position.averageCostPerShare) * this._position.numberOfShares
    }

    getUnrealizedGainPct() {
        if (!this.quote) {
            return null
        }
        return (this.quote.price - this._position.averageCostPerShare) / this._position.averageCostPerShare
    }
}

