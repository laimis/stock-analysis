import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOptionOrder, OptionContract, OptionPosition, OptionService} from "../../services/option.service";
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, NgIf, PercentPipe} from "@angular/common";
import {FormControl, FormsModule, ReactiveFormsModule} from "@angular/forms";
import {KeyValuePair} from "../../services/stocks.service";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";
import {GetErrors} from "../../services/utils";
import {OptionBrokerageOrdersComponent} from "../option-dashboard/option-brokerage-orders.component";

@Component({
  selector: 'app-option-position',
    imports: [
        NgClass,
        CurrencyPipe,
        DatePipe,
        ReactiveFormsModule,
        FormsModule,
        DecimalPipe,
        StockLinkAndTradingviewLinkComponent,
        PercentPipe,
        NgIf,
        OptionBrokerageOrdersComponent
    ],
  templateUrl: './option-position.component.html',
  styleUrl: './option-position.component.css'
})
export class OptionPositionComponent {
    constructor(
        private optionService: OptionService
    ) {
    }
    showDetailsModal: boolean = false;
    showAddLabelForm: boolean = false;
    newLabelKey: string;
    newLabelValue: string;
    showNotesForm: boolean = false;
    notesExpanded: boolean = false;
    notesControl = new FormControl();
    
    @Input() position: OptionPosition;
    @Input()
    set orders(value : BrokerageOptionOrder[]) {
        if (this.position == null) {
            return;
        }
        this.positionOrders = value.filter(o => o.legs[0].underlyingTicker === this.position.underlyingTicker);
    }
    get orders() : BrokerageOptionOrder[] {
        return this.positionOrders;
    }
    positionOrders: BrokerageOptionOrder[] = [];
    
    @Output() positionDeleted = new EventEmitter();
    @Output() positionChanged = new EventEmitter();
    @Output() errorOccurred = new EventEmitter<string[]>();

    showContractDetails(contract:OptionContract) {
        this.showDetailsModal = true;
    }
    
    openTradeModal() {
    }

    openCloseModal() {
    }
    
    removeLabel(label:KeyValuePair) {
        
        this.optionService.deleteLabel(this.position.positionId, label.key).subscribe({
            next: (result) => {
                this.position.labels = this.position.labels.filter(l => l.key != label.key);
                this.positionChanged.emit();
            },
            error: (error) => {
                let errors = GetErrors(error);
                this.errorOccurred.emit(errors);
            },
            complete: () => {
                console.log("Delete label complete");
            }
        });
    }
    
    addLabel() {
        this.optionService.setLabel(this.position.positionId, this.newLabelKey, this.newLabelValue).subscribe({
            next: (result) => {
                this.position.labels.push({key: this.newLabelKey, value: this.newLabelValue});
                this.positionChanged.emit();
            },
            error: (error) => {
                let errors = GetErrors(error);
                this.errorOccurred.emit(errors);
            },
            complete: () => {
                console.log("Add label complete");
            }
        });
    }

    closeDetailsModal() {
        this.showDetailsModal = false
    }
    
    toggleNotes() {
        this.notesExpanded = !this.notesExpanded;
    }
    
    addNote() {
    }
    
    closePositionWithMarketOrder() {
        
    }
    
    deletePosition() {
        if (confirm("Are you sure you want to delete this position?")) {
            this.optionService.delete(this.position.positionId).subscribe({
                next: (result) => {
                    console.log("Deleted position: " + this.position.positionId);
                    this.positionDeleted.emit();
                },
                error: (error) => {
                    console.error("Error deleting position: " + this.position.positionId);
                },
                complete: () => {
                    console.log("Delete position complete");
                }
            });
        }
    }

    transactionsExpanded = false;

    getTotalDebited(): number {
        return this.position.transactions.reduce((sum, t) => sum + t.debited, 0);
    }
    getTotalCredited(): number {
        return this.position.transactions.reduce((sum, t) => sum + t.credited, 0);
    }
    toggleTransactions(): void {
        this.transactionsExpanded = !this.transactionsExpanded;
    }


    protected readonly Math = Math;
}
