import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import {
    BrokerageAccount,
    BrokerageStockOrder, KeyValuePair,
    PositionEvent,
    StockPosition,
    StockQuote,
    StrategyProfitPoint
} from '../../services/stocks.service';
import {GetErrors, GetStockStrategies, toggleVisuallyHidden} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";
import {FormControl, FormsModule, ReactiveFormsModule} from "@angular/forms";
import { CurrencyPipe, DecimalPipe, NgClass, PercentPipe } from "@angular/common";
import {ParsedDatePipe} from "../../services/parsedDate.filter";
import {TradingViewLinkComponent} from "../../shared/stocks/trading-view-link.component";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {BrokerageNewOrderComponent} from "../../brokerage/brokerage-new-order.component";
import {BrokerageOrdersComponent} from "../../brokerage/brokerage-orders.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";

@Component({
    selector: 'app-stock-trading-position',
    templateUrl: './stock-trading-position.component.html',
    imports: [
    CurrencyPipe,
    ParsedDatePipe,
    FormsModule,
    PercentPipe,
    DecimalPipe,
    TradingViewLinkComponent,
    LoadingComponent,
    NgClass,
    BrokerageNewOrderComponent,
    BrokerageOrdersComponent,
    ReactiveFormsModule,
    ErrorDisplayComponent
],
    styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
    private stockService = inject(StockPositionsService);

    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0
    numberOfProfitPoints: number = 4
    positionStrategy: string = null
    strategies: { key: string; value: string; }[]
    showOrderForm: boolean = false;
    editingRisk: boolean = false;
    editingStrategy: boolean = false;
    eventsExpanded: boolean = false;
    gradingError: string = null
    gradingSuccess: string = null
    assignedGrade: string = null
    assignedNote: string = null
    gradeNoteText: string = ''
    
    @Input()
    notesExpanded = false
    @Input()
    quote: StockQuote
    @Input()
    brokerageAccount: BrokerageAccount | null = null
    @Input()
    positionOrders: BrokerageStockOrder[] = []
    @Output()
    positionChanged = new EventEmitter()

    // constructor that takes stock service
    constructor() {
        this.strategies = GetStockStrategies()
    }

    _position: StockPosition;

    @Input()
    set position(v: StockPosition) {
        this._position = v

        if (this._position) {
            this.positionStrategy = v.labels.find(l => l.key == "strategy")?.value
            this.positionProfitPoints = []
            this.assignedGrade = this._position.grade
            this.assignedNote = this._position.gradeNote
            this.gradeNoteText = this._position.gradeNote || ''
            this.setCandidateValues()
        }
    }
    
    toggleOrders() {
        this.showOrderForm = !this.showOrderForm
    }

    positionProfitPoints: StrategyProfitPoint[] = []
    showProfitPoints = false;
    fetchProfitPoints() {
        if (this.showProfitPoints) {
            this.showProfitPoints = false
            return
        }
        this.showProfitPoints = true
        this.stockService.getStrategyProfitPoints(
            this._position.positionId,
            this.numberOfProfitPoints).subscribe(
            (profitPoints) => {
                this.positionProfitPoints = profitPoints
            }, (err) => {
                let errors = GetErrors(err)
                alert("Error fetching profit points: " + errors.join(", "))
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
                },
                error => {
                    let errors = GetErrors(error)
                    alert("Error deleting stop price: " + errors.join(", "))
                }
            )
        }

        return false
    }

    setRiskAmount() {
        if (confirm("Are you sure you want to set the risk amount?")) {
            this.stockService.setRiskAmount(this._position.positionId, this.candidateRiskAmount).subscribe(
                (_) => {
                    this._position.riskedAmount = this.candidateRiskAmount
                },
                error => {
                    let errors = GetErrors(error)
                    alert('Unable to set risk: ' + errors.join(", "))
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
                        this.positionChanged.emit()
                    },
                    err => {
                        let errors = GetErrors(err)
                        alert("Error deleting position: " + errors.join(", "))
                    })
        }
    }

    closePosition(closeReason:string) {
        this.stockService.issueClosingOrders(this._position.positionId, closeReason)
            .subscribe(
                (_) => {
                    this.positionChanged.emit()
                },
                err => {
                    let errors = GetErrors(err)
                    alert("Error closing position: " + errors.join(", "))
                })
    }

    reinvestDividend(activityId:string) {
        let price = prompt("Please provide the price at which the dividend will be reinvested")
        if (!price) {
            return
        }
        let numPrice = parseFloat(price)
        if (isNaN(numPrice)) {
            alert("Please provide a valid price")
            return
        }
        this.stockService.reinvestDividend(this._position.positionId, activityId, numPrice)
            .subscribe(
                (_) => {
                    this.positionChanged.emit()
                },
                err => {
                    let errors = GetErrors(err)
                    alert("Error reinvesting dividend: " + errors.join(", "))
                }
            )
        
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
                    this.showNotesForm = false
                    this._position.notes.push({id:'', created: (new Date()).toISOString(), content: this.notesControl.value})
                    this.notesControl.setValue('')
                },
                err => {
                    let errors = GetErrors(err)
                    alert("Error adding notes: " + errors.join(", "))
                }
            )
        }
    }
    
    toggleNotesExpanded() {
        console.log("toggling notes expanded: " + this.notesExpanded)
        this.notesExpanded = !this.notesExpanded
    }

    assignGrade(note: string) {
        this.assignedNote = note
        this.stockService.assignGrade(
            this._position.positionId,
            this.assignedGrade,
            note).subscribe(
            (_: any) => {
                this.gradingSuccess = "Grade assigned successfully"
                setTimeout(() => {
                    this.gradingSuccess = null
                }, 5000)
            },
            (error) => {
                let errors = GetErrors(error)
                this.gradingError = errors.join(', ')
            }
        );
    }

    deleteTransaction(transactionId: string) {
        if (confirm("are you sure you want to delete the transaction?")) {
            this.stockService.deleteTransaction(this._position.positionId, transactionId)
                .subscribe(
                    _ => {
                        // refresh UI somehow here, tbd
                    }, (err) => {
                        const errors = GetErrors(err);
                        alert("Error deleting transaction: " + errors.join(", "))
                    })
        }
    }

    clearStrategy() {
        if (!confirm("Are you sure you want to clear the strategy?")) {
            return false
        }

        this.stockService.deleteLabel(this._position.positionId, "strategy").subscribe(
            _ => {
                this.positionStrategy = null
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error clearing strategy: " + errors.join(", "))
            }
        )

        return false
    }

    setStrategy(strategy: string) {
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
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error setting strategy: " + errors.join(", "))
            }
        )
    }
    
    showAddLabelForm:boolean = false;
    newLabelKey: string = '';
    newLabelValue: string = '';
    removeLabel(pair:KeyValuePair) {
        this.stockService.deleteLabel(this._position.positionId, pair.key).subscribe(
            _ => {
                this._position.labels = this._position.labels.filter(l => l.key != pair.key)
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error removing label: " + errors.join(", "))
            }
        )
    }
    addLabel() {
        if (this.newLabelKey === '' || this.newLabelValue === '') {
            alert("Please provide a key and value for the label")
            return
        }

        let label = {
            key: this.newLabelKey,
            value: this.newLabelValue
        }

        this.stockService.setLabel(this._position.positionId, label).subscribe(
            _ => {
                this._position.labels.push(label)
                this.newLabelKey = ''
                this.newLabelValue = ''
                this.showAddLabelForm = false
            },
            (err) => {
                const errors = GetErrors(err)
                alert("Error adding label: " + errors.join(", "))
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

    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;
}

