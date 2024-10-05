import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageOptionOrder, BrokerageStockOrder} from 'src/app/services/stocks.service';
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

}
