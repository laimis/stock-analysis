import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BrokerageOrder, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-option-brokerage-orders',
  templateUrl: './option-brokerage-orders.component.html',
  styleUrls: ['./option-brokerage-orders.component.css']
})

export class OptionBrokerageOrdersComponent {
  errors: string[];
  constructor(
    private service : StocksService
  ) {}

  @Input()
  orders : BrokerageOrder[]

  @Output()
  ordersUpdated = new EventEmitter()

  cancelOrder(order:BrokerageOrder){
      
    this.service.brokerageCancelOrder(order.orderId).subscribe( r => {
        this.ordersUpdated.emit()
      }, err => {
        this.errors = GetErrors(err)
      })
  }
  
}
