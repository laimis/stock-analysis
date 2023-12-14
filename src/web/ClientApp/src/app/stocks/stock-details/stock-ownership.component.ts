import { Component, Input, Output, EventEmitter } from '@angular/core';
import {
  StocksService,
  StockDetails,
  StockOwnership,
  PositionInstance,
  StockQuote,
  stocktransactioncommand
} from '../../services/stocks.service';
import { Router } from '@angular/router';

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


  public errors: string[]

  numberOfShares: number
	filled:         string
  notes:          string
}
