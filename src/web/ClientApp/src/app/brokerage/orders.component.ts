import { Component, Output, EventEmitter, OnInit, Input } from '@angular/core';
import { BrokerageOrder, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';
import { GetErrors } from '../services/utils';


@Component({
  selector: 'app-brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class BrokerageOrdersComponent implements OnInit {
  groupedOrders: BrokerageOrder[][];
  private _orders: BrokerageOrder[] = [];
  private _filteredTickers: string[] = [];
  isEmpty: boolean = false;
  error: string;

  constructor(
    private stockService: StocksService
  ) { }


  ngOnInit(): void {
    this.refreshOrders()
  }

  @Input()
  set filteredTickers(val:string[]) {
    this._filteredTickers = val
    this.groupAndRenderOrders()
  }
  get filteredTickers():string[] {
    return this._filteredTickers
  }

  refreshOrders() {
    this.stockService.brokerageAccount().subscribe(account => {
      this._orders = account.orders
      this.groupAndRenderOrders()
    },
      (err) => {
        let errors = GetErrors(err)
        // concat errors with comma separated
        this.error = errors.join(", ")
      }
    )
  }

  groupAndRenderOrders() {
    let isTickerVisible = (ticker) => this.filteredTickers.length === 0 || this.filteredTickers.indexOf(ticker) !== -1
    let isFilled = (o) => o.status === 'FILLED'
    let isBuy = (o) => o.type === 'BUY'
    let isSell = (o) => o.type === 'SELL'
    let orderBy = (a, b) => a.ticker.localeCompare(b.ticker)

    var buys = this._orders.filter(o => isBuy(o) && !isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var sells = this._orders.filter(o => isSell(o) && !isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var filledBuys = this._orders.filter(o => isBuy(o) && isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    var filledSells = this._orders.filter(o => isSell(o) && isFilled(o) && isTickerVisible(o.ticker)).sort(orderBy);
    this.groupedOrders = [buys, sells, filledBuys, filledSells]
    this.isEmpty = this.groupedOrders.every(o => o.length == 0)
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
      notes: null,
      stopPrice: null,
      brokerageOrderId: order.orderId,
      strategy: null
    }

    if (order.type === 'BUY') {
      this.stockService.purchase(obj).subscribe(() => {
        this.orderExecuted.emit("recorded buy")
        this.refreshOrders()
      }, (err) => {
        console.log(err)
      })
    }
    else if (order.type === 'SELL') {
      this.stockService.sell(obj).subscribe(() => {
        this.orderExecuted.emit("recorded sell")
        this.refreshOrders()
      }, (err) => {
        console.log(err)
      })
    }
  }

  getTotal(orders:BrokerageOrder[]) {
    return orders
      .filter(o => o.status !== 'CANCELED')
      .reduce((total, order) => total + order.price * order.quantity, 0)
  }
}
