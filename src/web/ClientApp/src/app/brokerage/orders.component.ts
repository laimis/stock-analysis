import { Component, Output, EventEmitter, OnInit } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent implements OnInit {
  groupedOrders: BrokerageOrder[][];

  constructor(
    private stockService: StocksService
  ) { }


  ngOnInit(): void {
    this.refreshOrders()
  }

  refreshOrders() {
    this.stockService.brokerageOrders().subscribe(orders => {
      var buys = orders.filter(o => o.type == 'BUY' && o.status !== 'FILLED');
      var sells = orders.filter(o => o.type == 'SELL' && o.status !== 'FILLED');
      var filled = orders.filter(o => o.status == 'FILLED');

      this.groupedOrders = [buys, sells, filled]
    });
  }

  @Output()
  orderExecuted: EventEmitter<string> = new EventEmitter<string>()

  cancelOrder(orderId: string) {
    this.stockService.brokerageCancelOrder(orderId).subscribe(() => {
      this.refreshOrders()
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
        this.orderExecuted.emit("recorded buy")
      }, (err) => {
        console.log(err)
      })
    }
    else if (order.type === 'SELL') {
      this.stockService.sell(obj).subscribe(() => {
        this.orderExecuted.emit("recorded sell")
      }, (err) => {
        console.log(err)
      })
    }
  }

  getTotal(orders:BrokerageOrder[]) {
    return orders
      .reduce((total, order) => total + order.price * order.quantity, 0)
  }
}
