import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BrokerageOrder, StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent {

  constructor(
    private stockService: StocksService
  ) { }

  @Input()
  orders: BrokerageOrder[]

  @Output()
  orderCancelled: EventEmitter<string> = new EventEmitter<string>()

  cancelOrder(orderId: string) {
    this.stockService.brokerageCancelOrder(orderId).subscribe(() => {
      this.orderCancelled.emit("cancelled")
    }, (err) => {
      console.log(err)
    }
    )
  }

  getTotalForBuys() {
    return this.orders
      .filter(o => o.isActive)
      .filter(o => o.type == 'BUY')
      .reduce((total, order) => total + order.price, 0)
  }
}
