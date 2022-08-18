import { Component, Input, Output, EventEmitter } from '@angular/core';
import { StocksService, GetErrors, StockDetails, StockOwnership, stocktransactioncommand } from '../../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'stock-ownership',
  templateUrl: './stock-ownership.component.html',
  styleUrls: ['./stock-ownership.component.css'],
})
export class StockOwnershipComponent {

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
    private router: Router
  ) { }

  showErrors(errors) {
    this.errors = errors
  }

  categoryChanged(elem: EventTarget) {
    var value = (elem as HTMLInputElement).value
    this.service.settings(this.ownership.ticker, value).subscribe( _ => {
      
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  transactionRecorded(type:string) {
    this.ownershipChanged.emit(type)
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

}
