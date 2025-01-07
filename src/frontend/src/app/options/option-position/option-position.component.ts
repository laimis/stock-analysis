import {Component, Input} from '@angular/core';
import {OptionContract, OptionPosition} from "../../services/option.service";
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass} from "@angular/common";
import {FormControl, FormsModule, ReactiveFormsModule} from "@angular/forms";
import {KeyValuePair} from "../../services/stocks.service";

@Component({
  selector: 'app-option-position',
    imports: [
        NgClass,
        CurrencyPipe,
        DatePipe,
        ReactiveFormsModule,
        FormsModule,
        DecimalPipe
    ],
  templateUrl: './option-position.component.html',
  styleUrl: './option-position.component.css'
})
export class OptionPositionComponent {

    showDetailsModal: boolean = false;
    showAddLabelForm: boolean = false;
    newLabelKey: string;
    newLabelValue: string;
    showNotesForm: boolean = false;
    notesExpanded: boolean = false;
    notesControl = new FormControl(); 
    selectedContract: OptionContract;
    
    @Input() position: OptionPosition;

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
    
}
