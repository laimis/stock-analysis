import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  BrokerageOrder,
  openpositioncommand,
  stocktransactioncommand,
  StockViolation
} from 'src/app/services/stocks.service';
import {toggleVisuallyHidden} from "../../services/utils";

@Component({
  selector: 'app-stock-violations',
  templateUrl: './stock-violations.component.html',
  styleUrls: ['./stock-violations.component.css']
})
export class StockViolationsComponent {

  tickersInViolations:string[] = []
  private _violations:StockViolation[] = []
  @Input()
  set violations(value:StockViolation[]) {
    this._violations = value
    this.tickersInViolations = value.map(v => v.ticker)
  }
  get violations() {
    return this._violations
  }

  @Input()
  orders : BrokerageOrder[] = []

  @Output()
  refreshRequested = new EventEmitter<string>()

  constructor() { }

  positionOpened(val:openpositioncommand) {
    this.refreshRequested.emit(val.ticker)
  }

  orderExecuted(ticker:string) {
    this.refreshRequested.emit(ticker)
  }

  tickerHasOrders(ticker:string) {
    return this.orders.some(o => o.ticker == ticker)
  }
  
  getDiffPrice(v:StockViolation) {
      if (v.numberOfShares > 0) {
          return (v.currentPrice - v.pricePerShare) / v.pricePerShare
      } else {
            return (v.pricePerShare - v.currentPrice) / v.currentPrice
      }
  }

    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;
}
