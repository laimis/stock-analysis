import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent {
  groupedOrders: BrokerageOrder[][];

  constructor(
    private stockService: StocksService
  ) { }

  @Input()
  set orders (value: BrokerageOrder[]) {
    var buys = value.filter(o => o.type == 'BUY' && o.status !== 'FILLED');
    var sells = value.filter(o => o.type == 'SELL' && o.status !== 'FILLED');
    var filled = value.filter(o => o.status == 'FILLED');

    this.groupedOrders = [buys, sells, filled]
  }

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

  recordOrder(order: BrokerageOrder) {
    var obj:stocktransactioncommand = {
      ticker: order.ticker,
      numberOfShares: order.quantity,
      price: order.price,
      date: order.date,
      notes: "Brokerage Order " + order.orderId,
      stopPrice: null
    }

    if (order.type === 'BUY') {
      this.stockService.purchase(obj).subscribe(() => {
        this.orderCancelled.emit("recorded buy")
      }, (err) => {
        console.log(err)
      })
    }
    else if (order.type === 'SELL') {
      this.stockService.sell(obj).subscribe(() => {
        this.orderCancelled.emit("recorded sell")
      }, (err) => {
        console.log(err)
      })
    }
  }

  getTotal(orders:BrokerageOrder[]) {
    return orders
      .reduce((total, order) => total + order.price, 0)
  }
}
