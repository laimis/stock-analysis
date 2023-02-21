import { Component, EventEmitter, Input, Output } from '@angular/core';
import { stocktransactioncommand, StockViolation } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-violations',
  templateUrl: './stock-violations.component.html',
  styleUrls: ['./stock-violations.component.css']
})
export class StockViolationsComponent {

  @Input() violations:StockViolation[] = []

  @Output() refreshRequested = new EventEmitter<string>()
  
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
    this.refreshRequested.emit(val.ticker)
  }
}
