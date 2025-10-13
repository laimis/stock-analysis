import { Component, EventEmitter, HostListener, Input, OnChanges, Output, inject } from '@angular/core';
import { CurrencyPipe, PercentPipe } from "@angular/common";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {OptionPosition, OptionService} from "../../../services/option.service";
import {GetErrors} from "../../../services/utils";
import {BrokerageService, OptionOrderCommand, OptionOrderInstruction, OptionOrderType} from "../../../services/brokerage.service";

@Component({
  selector: 'app-option-position-close-modal',
    imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    FormsModule,
    PercentPipe
],
  templateUrl: './option-position-close-modal.component.html',
  styleUrl: './option-position-close-modal.component.css'
})
export class OptionPositionCloseModalComponent implements OnChanges {
    private brokerageService = inject(BrokerageService);
    private optionService = inject(OptionService);

    /** Inserted by Angular inject() migration for backwards compatibility */
    constructor(...args: unknown[]);

    constructor() {
    }

    @Input() position: OptionPosition;
    @Input() isVisible: boolean = false;
    @Output() isVisibleChange = new EventEmitter<boolean>();
    @Output() positionOrderCreated = new EventEmitter();
    @Output() errorsOccurred = new EventEmitter<string[]>();

    positionNotes: string;
    price: number;
    quantity: number;
    spreadValues: { [key: string]: number } = {};

    ngOnChanges() {
        if (this.position) {
            console.log("setting price value", this.position.market)
            this.quantity = Math.abs(this.position.contracts[0].quantity)
            this.price = this.position.market / this.quantity;
            this.calculateSpreadValues();
        }
    }

    calculateSpreadValues() {
        const spread = this.position.spread;
        this.spreadValues = {
            '50%': spread * 0.5,
            '60%': spread * 0.6,
            '70%': spread * 0.7,
            '80%': spread * 0.8
        };
    }
    
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

    closePosition() {
        this.createClosePositionOrder(this.position)
    }

    createClosePositionOrder(position: OptionPosition) {
        // this will need to be completely rewritten
        let price = this.price
        let orderType = price < 0 ? OptionOrderType.NET_DEBIT : OptionOrderType.NET_CREDIT
        let session = "NORMAL"
        let duration = "GOOD_TILL_CANCEL"
        let orderStrategyType = "SINGLE"

        let collections = position.contracts.map(x => {
            return {
                instruction: x.quantity < 0 ? OptionOrderInstruction.BUY_TO_CLOSE : OptionOrderInstruction.SELL_TO_CLOSE,
                quantity: Math.abs(this.quantity),
                instrument: {
                    symbol: x.details.symbol,
                    assetType: "OPTION"
                }
            };
        });

        let order: OptionOrderCommand = {
            orderType,
            session,
            price,
            duration,
            orderStrategyType,
            orderLegCollection: collections
        }
        
        console.log("Option order", order)
        
        this.brokerageService.issueOptionOrder(order).subscribe({
                next: (_) => {
                    console.log('next', position)
                    this.postCloseNotes(position)
                },
                error: (err) => {
                    console.log('error', err)
                    this.errorsOccurred.emit(GetErrors(err))
                },
                complete: () => {
                    console.log('complete')
                    this.close()
                }
            }
        )
    }
    
    postCloseNotes(position: OptionPosition) {
        this.optionService.addNotes(position.positionId, this.positionNotes).subscribe({
            next: _ => {
                this.positionOrderCreated.emit()
                this.close()
            },
            error: (err) => {
                this.errorsOccurred.emit(GetErrors(err))
            },
            complete: () => {
                this.close()
            }
        })
    }
}
