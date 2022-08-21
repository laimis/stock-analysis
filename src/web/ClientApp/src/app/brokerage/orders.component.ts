import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { BrokerageOrder, StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class StockTradingPendingComponent implements OnInit {

  constructor(
    private stockService: StocksService
  ) { }

	ngOnInit() {
  }

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

  getTotalPrice() {
    return this.orders
      .filter(o => o.isActive).reduce((total, order) => total + order.price, 0)
  }
}
