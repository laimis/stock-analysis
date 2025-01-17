import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageStockOrder, PositionChartInformation, StockPosition, StockQuote} from '../../services/stocks.service';

@Component({
    selector: 'app-stock-ownership',
    templateUrl: './stock-ownership.component.html',
    styleUrls: ['./stock-ownership.component.css'],
    standalone: false
})
export class StockOwnershipComponent {

    @Input()
    positions: StockPosition[];

    @Input()
    quote: StockQuote

    @Input()
    positionChartInformation: PositionChartInformation

    @Output()
    positionChanged: EventEmitter<any> = new EventEmitter();
}
