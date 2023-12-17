import { Component, Output, EventEmitter, OnInit, Input } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';
import { GetErrors } from '../services/utils';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent {
  groupedOrders: BrokerageOrder[][];
  private _orders: BrokerageOrder[] = [];
  private _filteredTickers: string[] = [];
  isEmpty: boolean = false;
  error: string;

  @Output()
  orderCancelled:EventEmitter<string>
  @Output()
  purchaseRequested:EventEmitter<stocktransactioncommand>
  @Output()
  sellRequested:EventEmitter<stocktransactioncommand>

  @Input()
  justOrders: boolean = false;

  @Input()
  set orders(value:BrokerageOrder[]) {
    this._orders = value
    this.groupAndRenderOrders()
  }

  @Input()
  set filteredTickers(val:string[]) {
    this._filteredTickers = val
    this.groupAndRenderOrders()
  }

  @Input()
  positionId:string

  get filteredTickers():string[] {
    return this._filteredTickers
  }

  groupAndRenderOrders() {
    let isTickerVisible = (ticker:string) => this.filteredTickers.length === 0 || this.filteredTickers.indexOf(ticker) !== -1
    let isFilled = (o:BrokerageOrder) => o.status === 'FILLED'
    let isBuy = (o:BrokerageOrder) => o.type === 'BUY'
    let isSell = (o:BrokerageOrder) => o.type === 'SELL'
    let orderBy = (a:BrokerageOrder, b:BrokerageOrder) => a.ticker.localeCompare(b.ticker)

    var buys = this._orders.filter(o => isBuy(o) && !isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var sells = this._orders.filter(o => isSell(o) && !isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var filledBuys = this._orders.filter(o => isBuy(o) && isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var filledSells = this._orders.filter(o => isSell(o) && isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    this.groupedOrders = [buys, sells, filledBuys, filledSells]
    this.isEmpty = this.groupedOrders.every(o => o.length == 0)
  }

  cancelOrder(orderId: string) {
    this.orderCancelled.emit(orderId)
  }

  recordOrder(order: BrokerageOrder) {
    const obj: stocktransactioncommand = {
      positionId: this.positionId,
      numberOfShares: order.quantity,
      price: order.price,
      date: order.date,
      stopPrice: null,
      brokerageOrderId: order.orderId
    };

    if (order.type === 'BUY') {
      this.purchaseRequested.emit(obj)
    }
    else if (order.type === 'SELL') {
      this.sellRequested.emit(obj)
    }
  }

  getTotal(orders:BrokerageOrder[]) {
    return orders
      .filter(o => o.status !== 'CANCELED')
      .reduce((total, order) => total + order.price * order.quantity, 0)
  }
}
