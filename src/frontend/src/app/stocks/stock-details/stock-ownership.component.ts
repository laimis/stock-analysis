import {Component, EventEmitter, Input, Output} from '@angular/core';
import {BrokerageStockOrder, PositionChartInformation, StockPosition, StockQuote} from '../../services/stocks.service';
import {ParsedDatePipe} from "../../services/parsedDate.filter";
import {CurrencyPipe} from "@angular/common";
import {PriceChartComponent} from "../../shared/price-chart/price-chart.component";
import {StockTradingPositionComponent} from "../stock-trading/stock-trading-position.component";

@Component({
    selector: 'app-stock-ownership',
    templateUrl: './stock-ownership.component.html',
    imports: [
        ParsedDatePipe,
        CurrencyPipe,
        PriceChartComponent,
        StockTradingPositionComponent
    ],
    styleUrls: ['./stock-ownership.component.css']
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
