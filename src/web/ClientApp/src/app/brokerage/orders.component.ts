import { Component, Output, EventEmitter, OnInit, Input } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent implements OnInit {
  groupedOrders: BrokerageOrder[][];
  private _orders: BrokerageOrder[] = [];
  private _ticker: string;

  constructor(
    private stockService: StocksService
  ) { }


  ngOnInit(): void {
    this.refreshOrders()
  }

  @Input()
  set ticker(val:string) {
    this._ticker = val
    this.groupAndRenderOrders()
  }
  get ticker():string {
    return this._ticker
  }

  refreshOrders() {
    this.stockService.brokerageOrders().subscribe(orders => {
      this._orders = orders
      
      this.groupAndRenderOrders()
    });
  }
  groupAndRenderOrders() {
    var buys = this._orders.filter(o => o.type == 'BUY' && o.status !== 'FILLED' && o.ticker === (this.ticker ? this.ticker : o.ticker));
    var sells = this._orders.filter(o => o.type == 'SELL' && o.status !== 'FILLED' && o.ticker === (this.ticker ? this.ticker : o.ticker));
    var filledBuys = this._orders.filter(o => o.type == 'BUY' && o.status == 'FILLED' && o.ticker === (this.ticker ? this.ticker : o.ticker));
    var filledSells = this._orders.filter(o => o.type == 'SELL' && o.status == 'FILLED' && o.ticker === (this.ticker ? this.ticker : o.ticker));

    this.groupedOrders = [buys, sells, filledBuys, filledSells]
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
