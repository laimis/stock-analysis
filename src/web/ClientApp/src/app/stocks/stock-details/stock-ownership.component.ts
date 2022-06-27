import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { StocksService, GetErrors, StockDetails, StockOwnership, stocktransactioncommand } from '../../services/stocks.service';
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
  
  numberOfShares: number
	pricePerShare:  number
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

  categoryChanged(newCategory) {
    this.service.settings(this.ownership.ticker, newCategory).subscribe( _ => {
      alert("all good")
    }, err => {
      this.errors = GetErrors(err)
    })
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
      })
    }
  }

  record() {

    this.errors = null;
    
    var op = new stocktransactioncommand()
    op.ticker = this.stock.ticker
    op.numberOfShares = this.numberOfShares
    op.price = this.pricePerShare
    op.date = this.filled
    op.notes = this.notes

    if (this.positionType == 'buy') this.recordBuy(op)
    if (this.positionType == 'sell') this.recordSell(op)
  }

  recordBuy(stock: stocktransactioncommand) {
    this.service.purchase(stock).subscribe( _ => {
      this.ownershipChanged.emit("buy")
      this.clearFields()
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  recordSell(stock: stocktransactioncommand) {
    this.service.sell(stock).subscribe( _ => {
      this.ownershipChanged.emit("sell")
      this.clearFields()
    }, err => {
      this.errors = GetErrors(err)
    })
  }

}
