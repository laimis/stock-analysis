import {Component, EventEmitter, Input, Output} from '@angular/core';
import {
    StockDetails,
    StockOwnership,
    PositionInstance,
    StockQuote,
    stocktransactioncommand, BrokerageOrder
} from '../../services/stocks.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-stock-ownership',
  templateUrl: './stock-ownership.component.html',
  styleUrls: ['./stock-ownership.component.css'],
})
export class StockOwnershipComponent {

  @Input()
  positions: PositionInstance[];

  @Input()
  quote: StockQuote
    
    @Input()
    orders: BrokerageOrder[]
    
    @Output()
    ordersChanged: EventEmitter<string> = new EventEmitter<string>();

}
