import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  BrokerageOrder,
  openpositioncommand,
  stocktransactioncommand,
  StockViolation
} from 'src/app/services/stocks.service';

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

  @Output()
  sellRequested = new EventEmitter<stocktransactioncommand>()

  @Output()
  purchaseRequested = new EventEmitter<stocktransactioncommand>()

  activeTicker: string = null
  fixType : string = null

  constructor() { }

  recordTransaction(ticker:string) {
    this.activeTicker = ticker
    this.fixType = 'transaction'
  }

  openPosition(ticker:string) {
    this.activeTicker = ticker
    this.fixType = 'position'
  }

  transactionFailed(errors) {
    alert("failures" + errors.join("\n"))
  }

  transactionRecorded(val:stocktransactioncommand) {
    this.refreshRequested.emit(val.positionId)
  }

  positionOpened(val:openpositioncommand) {
    this.refreshRequested.emit(val.ticker)
  }

  orderExecuted(ticker:string) {
    this.refreshRequested.emit(ticker)
  }

  tickerHasOrders(ticker:string) {
    return this.orders.some(o => o.ticker == ticker)
  }
}
