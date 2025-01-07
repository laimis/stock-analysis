import {Component, EventEmitter, Input, Output} from '@angular/core';
import {OptionContract, OptionPosition, OptionService} from "../../services/option.service";
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass} from "@angular/common";
import {FormControl, FormsModule, ReactiveFormsModule} from "@angular/forms";
import {KeyValuePair} from "../../services/stocks.service";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";

@Component({
  selector: 'app-option-position',
    imports: [
        NgClass,
        CurrencyPipe,
        DatePipe,
        ReactiveFormsModule,
        FormsModule,
        DecimalPipe,
        StockLinkAndTradingviewLinkComponent
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
    selectedContract: OptionContract;
    
    @Input() position: OptionPosition;
    
    @Output() positionDeleted = new EventEmitter();

    showContractDetails(contract:OptionContract) {
        this.showDetailsModal = true;
    }
    
    openTradeModal() {
    }

    openCloseModal() {
    }
    
    removeLabel(label:KeyValuePair) {
    }
    
    addLabel() {}

    closeDetailsModal() {
        this.showDetailsModal = false
    }
    
    toggleNotes() {
        this.notesExpanded = !this.notesExpanded;
    }
    
    addNote() {
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
}
