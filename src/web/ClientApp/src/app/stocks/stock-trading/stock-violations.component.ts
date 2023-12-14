import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';
import {openpositioncommand, stocktransactioncommand, StockViolation} from 'src/app/services/stocks.service';

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

  @Output() refreshRequested = new EventEmitter<string>()

  @ViewChild(BrokerageOrdersComponent)
  private brokerageOrdersComponent!: BrokerageOrdersComponent;

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
}
