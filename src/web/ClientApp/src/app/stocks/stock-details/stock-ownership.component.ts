import { Component, Input } from '@angular/core';
import {
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

  positions: PositionInstance[];

  @Input()
  public set ownership(value) {

    // create new array of positions that is
    // created from value.positions, but reversed in orders
    if (value)
    {
      this.positions = value.positions.slice().reverse()
    }
  }

  @Input()
  quote: StockQuote

}
