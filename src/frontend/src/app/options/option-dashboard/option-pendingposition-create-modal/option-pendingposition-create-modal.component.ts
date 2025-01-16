import {Component, EventEmitter, HostListener, Input, Output} from '@angular/core';
import {CurrencyPipe, NgForOf, NgIf} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {BrokerageOptionPosition, OptionLeg, OptionService} from "../../../services/option.service";
import {GetErrors, GetOptionStrategies} from "../../../services/utils";
import {
    BrokerageService,
    OptionOrderCommand,
    OptionOrderInstruction,
    OptionOrderType
} from "../../../services/brokerage.service";
import {ErrorDisplayComponent} from "../../../shared/error-display/error-display.component";

@Component({
  selector: 'app-option-pendingposition-create-modal',
    imports: [
        CurrencyPipe,
        FormsModule,
        NgForOf,
        NgIf,
        ErrorDisplayComponent
    ],
  templateUrl: './option-pendingposition-create-modal.component.html',
  styleUrl: './option-pendingposition-create-modal.component.css'
})
export class OptionPendingPositionCreateModalComponent {
    constructor(
        private optionService: OptionService,
        private brokerageService: BrokerageService
    ) {
        this.optionStrategies = GetOptionStrategies()
    }

    @Input() ticker: string
    @Input() selectedLegs: OptionLeg[];
    @Input() price: number;
    @Input() isVisible: boolean = false;
    @Output() isVisibleChange = new EventEmitter<boolean>();
    @Output() positionCreated = new EventEmitter();

    errors: string[] = []
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

    createPosition() {
        this.turnIntoPosition()
    }
    
    private brokerageOrder() {
        let orderType = this.price < 0 ? OptionOrderType.NET_DEBIT : OptionOrderType.NET_CREDIT
        let session = "NORMAL"
        let duration = "GOOD_TILL_CANCEL"
        let orderStrategyType = "SINGLE"

        let collections = this.selectedLegs.map(x => {
            return {
                instruction: x.action === "buy" ? OptionOrderInstruction.BUY_TO_OPEN : OptionOrderInstruction.SELL_TO_OPEN,
                quantity: x.quantity,
                instrument: {
                    symbol: x.option.symbol,
                    assetType: "OPTION"
                }
            }
        })

        let order : OptionOrderCommand = {
            orderType,
            session,
            price: this.price,
            duration,
            orderStrategyType,
            orderLegCollection: collections
        }
        console.log("I would issue brokerage order now", order)

        this.brokerageService.issueOptionOrder(order).subscribe(
            (data) => {
                alert("Order created")
                this.close()
                this.positionCreated.emit()
            },
            (error) => {
                console.log("Error creating order", error)
                this.errors = GetErrors(error)
            }
        )
    }

    turnIntoPosition() {
        console.log('mapping pending position' )
        // this will need to be completely rewritten
        let command = {

            underlyingTicker: this.ticker,
            cost: this.price,
            notes: this.positionNotes,
            strategy: this.positionStrategy,
            contracts: this.selectedLegs.map(l => ({
                quantity: l.quantity,
                strikePrice: l.option.strikePrice,
                expirationDate: l.option.expiration,
                optionType: l.option.optionType
            }))
        }
        
        console.log('command', command)

        this.optionService.openpending(command).subscribe({
                next: (position) => {
                    console.log('next', position)
                    this.brokerageOrder()
                },
                error: (err) => {
                    console.log('error', err)
                    this.errors = GetErrors(err)
                },
                complete: () => {
                    console.log('complete')
                }
            }
        )
    }
}
