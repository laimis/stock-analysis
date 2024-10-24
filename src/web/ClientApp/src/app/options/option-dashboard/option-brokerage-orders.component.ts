import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOptionOrder, BrokerageStockOrder, OptionOrderLeg} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {BrokerageService} from "../../services/brokerage.service";

@Component({
    selector: 'app-option-brokerage-orders',
    templateUrl: './option-brokerage-orders.component.html',
    styleUrls: ['./option-brokerage-orders.component.css']
})

export class OptionBrokerageOrdersComponent {
    activeFilter: string = ''
    errors: string[];
    private _orders: BrokerageOptionOrder[];
    groupedOrders: Map<string, BrokerageOptionOrder[]> = new Map<string, BrokerageOptionOrder[]>()
    @Input()
    set orders(value: BrokerageOptionOrder[]) {
        this._orders = value
        this.createGroupedOrders()
    }
    get orders() {
        return this._orders
    }
    
    @Output()
    ordersUpdated = new EventEmitter()

    constructor(
        private service: BrokerageService
    ) {
    }

    cancelOrder(order: BrokerageOptionOrder) {
        if (!confirm('Are you sure you want to cancel this order?')) {
            return
        }
        this.service.cancelOrder(order.orderId).subscribe(r => {
            this.ordersUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
    }

    filterOrders(status: string) {
        this.activeFilter = this.activeFilter === status ? '' : status;
        this.createGroupedOrders()
    }
    
    createGroupedOrders() {
        this.groupedOrders = this._orders
            .filter(o => o.status === this.activeFilter || this.activeFilter === '')
            .reduce((a, b) => {
                const key = b.status;
                if (!a.has(key)) {
                    a.set(key, [])
                }
                let arr = a.get(key)
                arr.push(b)
                return a
            }, new Map<string, BrokerageOptionOrder[]>())
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
