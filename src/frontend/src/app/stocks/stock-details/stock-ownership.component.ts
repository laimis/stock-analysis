import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageStockOrder, PositionChartInformation, PositionInstance, StockQuote} from '../../services/stocks.service';

@Component({
    selector: 'app-stock-ownership',
    templateUrl: './stock-ownership.component.html',
    styleUrls: ['./stock-ownership.component.css'],
    standalone: false
})
export class StockOwnershipComponent {

    @Input()
    positions: PositionInstance[];

    @Input()
    quote: StockQuote

    @Input()
    orders: BrokerageStockOrder[]

    @Input()
    positionChartInformation: PositionChartInformation

    @Output()
    ordersChanged: EventEmitter<string> = new EventEmitter<string>();
    
    @Output()
    notesChanged: EventEmitter<string> = new EventEmitter<string>();
}
