import {Component, EventEmitter, Input, Output} from '@angular/core';
import {GetErrors} from 'src/app/services/utils';
import {BrokerageService} from "../../services/brokerage.service";
import {
    BrokerageOptionOrder,
    BrokerageOptionPosition, OptionContract,
    OptionOrderLeg,
    OptionPosition, OptionService
} from "../../services/option.service";
import {TradingViewLinkComponent} from "../../shared/stocks/trading-view-link.component";
import {StockLinkComponent} from "../../shared/stocks/stock-link.component";
import { CurrencyPipe, DecimalPipe, NgClass, PercentPipe } from "@angular/common";
import {ParsedDatePipe} from "../../services/parsedDate.filter";
import {
    OptionPositionCreateModalComponent
} from "./option-position-create-modal/option-position-create-modal.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";

@Component({
    selector: 'app-option-brokerage-orders',
    templateUrl: './option-brokerage-orders.component.html',
    styleUrls: ['./option-brokerage-orders.component.css'],
    imports: [
    TradingViewLinkComponent,
    StockLinkComponent,
    NgClass,
    CurrencyPipe,
    ParsedDatePipe,
    DecimalPipe,
    OptionPositionCreateModalComponent,
    ErrorDisplayComponent,
    PercentPipe
]
})

export class OptionBrokerageOrdersComponent {
    activeFilter: string;
    errors: string[];
    private _orders: Map<string, BrokerageOptionOrder[]>;
    availableOrderStatuses = ["Working", "PendingActivation", "Pending", "Filled", "Cancelled", "Expired", "Replaced"]
    
    @Input()
    set orders(value : BrokerageOptionOrder[]) {
        this._orders = 
            value.reduce((a, b) => {
                const key = b.status;
                if (!a.has(key)) {
                    a.set(key, [])
                }
                let arr = a.get(key)
                arr.push(b);
                return a
            }, new Map<string, BrokerageOptionOrder[]>())
        
        // find the first status that has orders and set it as the active filter
        let status = this.availableOrderStatuses.find(status => this._orders.has(status))
        
        this.filterOrders(status)
    }
    
    @Input()
    position: OptionPosition
    
    @Output()
    ordersUpdated = new EventEmitter()
    selectedOrders: BrokerageOptionOrder[];

    constructor(
        private service: BrokerageService,
        private optionService: OptionService
    ) {
    }

    cancelOrder(order: BrokerageOptionOrder) {
        if (!confirm('Are you sure you want to cancel this order?')) {
            return
        }
        this.service.cancelOrder(order.orderId).subscribe(_ => {
            this.ordersUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    filterOrders(status: string) {
        this.activeFilter = this.activeFilter === status ? '' : status;
        this.selectedOrders = this._orders.get(status)
    }

    applyOrderToPosition(order: BrokerageOptionOrder) {
        let isOpen = order.contracts.filter(leg => leg.instruction.endsWith('ToOpen'))
        let isClose = order.contracts.filter(leg => leg.instruction.endsWith('ToClose'))
        
        if (isOpen.length === 0 && isClose.length === 0) {
            this.errors = ['Order must have at least one open or close leg']
            return
        }

        let contracts =
            order.contracts.map(contract =>
                {
                    return {
                        optionType: contract.optionType,
                        strikePrice: contract.strikePrice,
                        expirationDate: contract.expiration,
                        quantity: contract.quantity,
                        cost: contract.cost,
                        filled: order.executionTime
                    }
                }
            )
        
        let promise = isOpen.length > 0 ? this.optionService.openContracts(this.position.positionId, contracts) : this.optionService.closeContracts(this.position.positionId, contracts) 
        
        promise.subscribe(
            {
                next: _ => {
                    this.ordersUpdated.emit()
                },
                error: err => {
                    this.errors = GetErrors(err)
                },
                complete: () => {
                    console.log('complete close contracts')
                }
            }
        );
    }
    
    selectedOption : BrokerageOptionPosition
    isModalVisible: boolean = false
    createPosition(order:BrokerageOptionOrder) {
        this.selectedOption = {
            cost: order.price,
            showPL: false,
            marketValue: undefined,
            brokerageContracts: order.contracts.map(contract => {
                return {
                    ticker: contract.underlyingTicker,
                    averageCost: contract.cost,
                    quantity: contract.quantity,
                    description: "",
                    optionType: contract.optionType,
                    strikePrice: contract.strikePrice,
                    marketValue: undefined,
                    expirationDate: contract.expiration
                }
            })
        }
        this.isModalVisible = true
    }
    
    marketPrice(legs:OptionContract[]) : number {
        // go through each leg and add up the price of each leg to the total
        // to determine what value to use, look at instruction property, if it says 
        // BuyTo* then the positive price value should be used, if it says SellTo* then the negative price value should be used
        
        let total = 0
        legs.forEach(leg => {
            if (leg.instruction.startsWith("BuyTo")) {
                total += leg.market
            } else if (leg.instruction.startsWith("SellTo")) {
                total -= leg.market
            }
        })
        return total
    }

}
