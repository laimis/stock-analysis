import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { StocksService, GetErrors, StockDetails, StockOwnership } from '../../services/stocks.service';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'stock-ownership',
  templateUrl: './stock-ownership.component.html',
  styleUrls: ['./stock-ownership.component.css'],
  providers: [DatePipe]
})
export class StockOwnershipComponent implements OnInit {

  @Input()
  public ownership: StockOwnership;

  @Input()
  public stock: StockDetails;

  @Output()
  ownershipChanged = new EventEmitter();

  public errors: string[]
  success : boolean

  numberOfShares: Number
	pricePerShare:  Number
	filled:         string
  positionType:   string
  notes:          string

  constructor(
    private service: StocksService,
    private router: Router,
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

  delete() {

    if (confirm("are you sure you want to delete this stock?"))
    {
      this.errors = null

      this.service.deleteStocks(this.ownership.id).subscribe(r => {
        this.router.navigateByUrl('/dashboard')
      })
    }
  }

  deleteTransaction(transactionId:string) {
    if (confirm("are you sure you want to delete the transaction?"))
    {
      this.errors = null

      this.service.deleteStockTransaction(this.ownership.id, transactionId).subscribe(_ => {
        this.ownershipChanged.emit("deletetransaction")
        this.success = true
      })
    }
  }

  record() {

    this.errors = null;
    this.success = false;

    var op = {
      ticker: this.stock.ticker,
      numberOfShares: this.numberOfShares,
      price: this.pricePerShare,
      date: this.filled,
      notes: this.notes
    }

    if (this.positionType == 'buy') this.recordBuy(op)
    if (this.positionType == 'sell') this.recordSell(op)
  }

  recordBuy(stock: object) {
    this.service.purchase(stock).subscribe( _ => {
      this.ownershipChanged.emit("buy")
      this.clearFields()
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  recordSell(stock: object) {
    this.service.sell(stock).subscribe( _ => {
      this.ownershipChanged.emit("sell")
      this.clearFields()
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

}
