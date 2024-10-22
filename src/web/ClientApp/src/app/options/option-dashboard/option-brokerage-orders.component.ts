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
    errors: string[];
    @Input()
    orders: BrokerageOptionOrder[]
    @Output()
    ordersUpdated = new EventEmitter()

    constructor(
        private service: BrokerageService
    ) {
    }

    cancelOrder(order: BrokerageOptionOrder) {

        this.service.cancelOrder(order.orderId).subscribe(r => {
            this.ordersUpdated.emit()
        }, err => {
            this.errors = GetErrors(err)
        })
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
