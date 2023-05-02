import { Component, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { StocksService, StockDetails, StockOwnership, PositionInstance } from '../../services/stocks.service';
import { Router } from '@angular/router';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-ownership',
  templateUrl: './stock-ownership.component.html',
  styleUrls: ['./stock-ownership.component.css'],
})
export class StockOwnershipComponent {

  private _ownership: StockOwnership
  positions: PositionInstance[];

  @Input()
  public set ownership(value) {
    this._ownership = value
    
    // create new array of positions that is 
    // created from value.positions, but reversed in orders
    if (value)
    {
      this.positions = value.positions.slice().reverse()
    }
    
  }
  public get ownership() {
    return this._ownership
  }

  @Input()
  public stock: StockDetails;

  @Output()
  ownershipChanged = new EventEmitter();

  @ViewChild(BrokerageOrdersComponent)
  private brokerageOrders!: BrokerageOrdersComponent;

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

  brokerageOrderEntered() {
    this.brokerageOrders.refreshOrders()
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
