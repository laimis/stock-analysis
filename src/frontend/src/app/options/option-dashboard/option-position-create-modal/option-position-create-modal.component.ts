import {Component, EventEmitter, HostListener, Input, Output} from '@angular/core';
import { CurrencyPipe } from "@angular/common";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {GetErrors, GetOptionStrategies} from "../../../services/utils";
import {BrokerageOptionPosition, OptionService} from "../../../services/option.service";

@Component({
  selector: 'app-option-position-create-modal',
    imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    FormsModule
],
  templateUrl: './option-position-create-modal.component.html',
  styleUrl: './option-position-create-modal.component.css'
})
export class OptionPositionCreateModalComponent {
    
    constructor(
        private optionService: OptionService
    ) {
        this.optionStrategies = GetOptionStrategies()
    }
    
    @Input() selectedOption: BrokerageOptionPosition;
    @Input() isVisible: boolean = false;
    @Output() isVisibleChange = new EventEmitter<boolean>();
    @Output() positionCreated = new EventEmitter();
    @Output() errorsOccurred = new EventEmitter<string[]>();

    positionNotes: string;
    positionStrategy: string;
    optionStrategies: { key: string, value: string }[] = []

    @HostListener('document:keydown.escape')
    onEscape() {
        this.close();
    }

    onOverlayClick(event: MouseEvent) {
        if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
            this.close();
        }
    }

    close() {
        this.isVisible = false;
        this.isVisibleChange.emit(this.isVisible);
    }

    createPosition(filledDate:string) {
        this.turnIntoPosition(this.selectedOption, filledDate)
    }

    turnIntoPosition(position: BrokerageOptionPosition, filledDate: string) {
        console.log('mapping', position, filledDate)
        // this will need to be completely rewritten
        let command = {

            underlyingTicker: position.brokerageContracts[0].ticker,
            filled: filledDate,
            notes: this.positionNotes,
            strategy: this.positionStrategy,
            contracts: position.brokerageContracts.map(l => ({
                quantity: l.quantity,
                strikePrice: l.strikePrice,
                expirationDate: l.expirationDate,
                optionType: l.optionType,
                cost: l.averageCost,
                filled: filledDate
            }))
        }

        this.optionService.open(command).subscribe({
                next: (position) => {
                    console.log('next', position)
                    this.close()
                    this.positionCreated.emit()
                },
                error: (err) => {
                    console.log('error', err)
                    this.errorsOccurred.emit(GetErrors(err))
                },
                complete: () => {
                    this.close()
                    console.log('complete')
                }
            }
        )
    }
}
