import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { StocksService, GetErrors, stocktransactioncommand } from '../../services/stocks.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-stock-transaction',
  templateUrl: './stock-transaction.component.html',
  styleUrls: ['./stock-transaction.component.css'],
  providers: [DatePipe]
})
export class StockTransactionComponent implements OnInit {

  @Input()
  ticker: string

  @Input()
  numberOfShares: number

  @Input()
  pricePerShare: number

  @Output()
  transactionRecorded = new EventEmitter();

  @Output()
  transactionFailed = new EventEmitter<string[]>()

  filled:         string
  positionType:   string
  notes:          string


  constructor(
    private service: StocksService,
    private datePipe: DatePipe
  ) { }

  ngOnInit() {
    this.filled = Date()
    this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
  }

  clearFields() {
    this.numberOfShares = null
	  this.pricePerShare = null
	  this.positionType = null
    this.notes = null
  }


  record() {

    var op = new stocktransactioncommand()
    op.ticker = this.ticker
    op.numberOfShares = this.numberOfShares
    op.price = this.pricePerShare
    op.date = this.filled
    op.notes = this.notes

    if (this.positionType == 'buy') this.recordBuy(op)
    if (this.positionType == 'sell') this.recordSell(op)
  }

  recordBuy(stock: stocktransactioncommand) {
    this.service.purchase(stock).subscribe( _ => {
      this.transactionRecorded.emit("buy")
      this.clearFields()
    }, err => {
      this.transactionFailed.emit(GetErrors(err))
    })
  }

  recordSell(stock: stocktransactioncommand) {
    this.service.sell(stock).subscribe( _ => {
      this.transactionRecorded.emit("sell")
      this.clearFields()
    }, err => {
      this.transactionFailed.emit(GetErrors(err))
    })
  }

}
