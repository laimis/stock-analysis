import { Component, EventEmitter, HostListener, Input, OnChanges, Output } from '@angular/core';
import {FormsModule} from "@angular/forms";
import {OptionContract, OptionPosition, OptionService} from "../../../services/option.service";
import {
    OptionOrderType,
    OptionOrderInstruction,
    OptionOrderCommand,
    BrokerageService,
} from "../../../services/brokerage.service";
import {GetErrors} from "../../../services/utils";
import { CurrencyPipe } from "@angular/common";

@Component({
  selector: 'app-option-contract-close-modal',
    imports: [
    FormsModule,
    CurrencyPipe
],
  templateUrl: './option-contract-close-modal.component.html',
  styleUrl: './option-contract-close-modal.component.css'
})
export class OptionContractCloseModalComponent implements OnChanges {
    @Input() position: OptionPosition;
    @Input() contract: OptionContract;
    @Input() isVisible: boolean = false;
    @Output() isVisibleChange = new EventEmitter<boolean>();
    @Output() contractOrderCreated = new EventEmitter();
    @Output() errorsOccurred = new EventEmitter<string[]>();

    positionNotes: string;
    price: number;
    quantity: number;
    costDrops: { [key: string]: number } = {};
    
    constructor(
        private optionService: OptionService,
        private brokerageService: BrokerageService
    ) {
    }

    ngOnChanges() {
        if (this.position && this.contract?.details) {
            console.log("setting price value", this.contract.details?.mark)
            this.price = this.contract.details.mark;
            this.quantity = Math.abs(this.contract.quantity);
            this.calculateCostDrops();
        }
    }

    calculateCostDrops() {
        const price = this.contract.cost;
        this.costDrops = {
            '70%': price - price * 0.7,
            '80%': price - price * 0.8,
            '90%': price - price * 0.9,
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

    closeContract() {
        this.createCloseContractOrder(this.position, this.contract);
    }

    createCloseContractOrder(position:OptionPosition, contract: OptionContract) {
        let price = this.price;
        let orderType = OptionOrderType.LIMIT
        let session = "NORMAL";
        let duration = "GOOD_TILL_CANCEL";
        let orderStrategyType = "SINGLE";
        let instruction = 
            contract.quantity < 0 ?
                OptionOrderInstruction.BUY_TO_CLOSE : OptionOrderInstruction.SELL_TO_CLOSE

        let order: OptionOrderCommand = {
            orderType: orderType,
            session: session,
            price: price,
            duration: duration,
            orderStrategyType: orderStrategyType,
            orderLegCollection: [
                {
                    instruction: instruction,
                    quantity: Math.abs(this.quantity),
                    instrument: {
                        symbol: contract.details.symbol,
                        assetType: "OPTION"
                    }
                }
            ]
        }

        console.log("Option contract order", order);

        this.brokerageService.issueOptionOrder(order).subscribe({
            next: (_) => {
                console.log('next', contract);
                this.postCloseNotes(position);
            },
            error: (err) => {
                console.log('error', err);
                this.errorsOccurred.emit(GetErrors(err));
            },
            complete: () => {
                console.log('complete');
                this.close();
            }
        });
    }

    postCloseNotes(position:OptionPosition) {
        this.optionService.addNotes(position.positionId, this.positionNotes).subscribe({
            next: _ => {
                this.contractOrderCreated.emit();
                this.close();
            },
            error: (err) => {
                this.errorsOccurred.emit(GetErrors(err));
            },
            complete: () => {
                this.close();
            }
        });
    }
}
