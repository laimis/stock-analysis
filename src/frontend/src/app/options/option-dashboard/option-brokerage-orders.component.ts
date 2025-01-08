import {Component, EventEmitter, Input, Output} from '@angular/core';
import {GetErrors} from 'src/app/services/utils';
import {BrokerageService} from "../../services/brokerage.service";
import {
    BrokerageOptionOrder,
    BrokerageOptionPosition,
    OptionOrderLeg,
    OptionPosition
} from "../../services/option.service";

@Component({
    selector: 'app-option-brokerage-orders',
    templateUrl: './option-brokerage-orders.component.html',
    styleUrls: ['./option-brokerage-orders.component.css'],
    standalone: false
})

export class OptionBrokerageOrdersComponent {
    activeFilter: string = 'Working'
    errors: string[];
    private _orders: Map<string, BrokerageOptionOrder[]>;
    
    @Input()
    set orders(value : Map<string, BrokerageOptionOrder[]>) {
        this._orders = value
        this.filterOrders(this.activeFilter)
    }
    get orders() {
        return this._orders
    }
    
    @Input()
    position: OptionPosition
    
    @Output()
    ordersUpdated = new EventEmitter()
    selectedOrders: BrokerageOptionOrder[];

    constructor(
        private service: BrokerageService
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
        this.selectedOrders = this.orders.get(status)
    }

    applyOrderToPosition(order: BrokerageOptionOrder) {
        
    }
    
    selectedOption : BrokerageOptionPosition
    isModalVisible: boolean = false
    createPosition(order:BrokerageOptionOrder) {
        this.selectedOption = {
            cost: order.price,
            showPL: false,
            marketValue: undefined,
            brokerageContracts: order.legs.map(leg => {
                return {
                    ticker: leg.underlyingTicker,
                    averageCost: leg.price,
                    quantity: leg.quantity,
                    description: leg.description,
                    optionType: leg.optionType,
                    strikePrice: leg.strikePrice,
                    marketValue: undefined,
                    expirationDate: leg.expiration
                }
            })
        }
        this.isModalVisible = true
    }
    
    marketPrice(legs:OptionOrderLeg[]) : number {
        // go through each leg and add up the price of each leg to the total
        // to determine what value to use, look at instruction property, if it says 
        // BuyTo* then the positive price value should be used, if it says SellTo* then the negative price value should be used
        
        let total = 0
        legs.forEach(leg => {
            if (leg.instruction.startsWith("BuyTo")) {
                total += leg.price
            } else if (leg.instruction.startsWith("SellTo")) {
                total -= leg.price
            }
        })
        return total
    }

}
