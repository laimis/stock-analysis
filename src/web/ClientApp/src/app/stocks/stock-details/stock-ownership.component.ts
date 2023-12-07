import { Component, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import {StocksService, StockDetails, StockOwnership, PositionInstance, StockQuote} from '../../services/stocks.service';
import { Router } from '@angular/router';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';

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
  stock: StockDetails;

  @Input()
  quote: StockQuote

  @Output()
  ownershipChanged = new EventEmitter();

  @ViewChild(BrokerageOrdersComponent)
  private brokerageOrders!: BrokerageOrdersComponent;

  public errors: string[]

  numberOfShares: number
	filled:         string
  notes:          string

  constructor(
    private service: StocksService,
    private router: Router
  ) { }

  brokerageOrderEntered() {
    this.brokerageOrders.refreshOrders()
  }

  brokerageOrderExecuted() {
    this.ownershipChanged.emit('orders')
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

}
