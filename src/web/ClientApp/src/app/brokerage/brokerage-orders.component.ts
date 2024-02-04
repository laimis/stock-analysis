import { Component, Output, EventEmitter, OnInit, Input } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';
import { GetErrors } from '../services/utils';
import {BrokerageService} from "../services/brokerage.service";

let orderBy = (a:BrokerageOrder, b:BrokerageOrder) => a.ticker.localeCompare(b.ticker)

@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './brokerage-orders.component.html',
  styleUrls: ['./brokerage-orders.component.css']
})
export class BrokerageOrdersComponent {
  groupedOrders: BrokerageOrder[][];
  private _orders: BrokerageOrder[] = [];
  private _filteredTickers: string[] = [];
  isEmpty: boolean = false;
  errors: string[];

  constructor(private brokerage:BrokerageService) {
  }

  @Output()
  purchaseRequested:EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()
  @Output()
  sellRequested:EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()
    @Output()
    cancelRequested:EventEmitter<string> = new EventEmitter<string>()

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

    if (this._orders) {
      var buys = this._orders.filter(o => o.isBuyOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
      var sells = this._orders.filter(o => o.isSellOrder && o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
      var filledBuys = this._orders.filter(o => o.isBuyOrder && !o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
      var filledSells = this._orders.filter(o => o.isSellOrder && !o.isActive && isTickerVisible(o.ticker)).sort(orderBy);
      this.groupedOrders = [buys, sells, filledBuys, filledSells]
      this.isEmpty = this.groupedOrders.every(o => o.length == 0)
    }
  }

  cancelOrder(orderId: string) {
    this.brokerage.cancelOrder(orderId)
      .subscribe(
        () => {
          this.brokerage.brokerageAccount().subscribe(
            a => {
                this.cancelRequested.emit(orderId)
                this.orders = a.orders
            },
            err => this.errors = GetErrors(err)
          )
        },
        err => this.errors = GetErrors(err)
      )
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

    if (order.isBuyOrder) {
      this.purchaseRequested.emit(obj)
    }
    else if (order.isSellOrder) {
      this.sellRequested.emit(obj)
    }
  }

  getTotal(orders:BrokerageOrder[]) {
    return orders
      .filter(o => o.status !== 'CANCELED')
      .reduce((total, order) => total + order.price * order.quantity, 0)
  }
}
