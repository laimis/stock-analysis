import {Component, Input} from '@angular/core';
import {StockDetails, StockProfile} from '../../services/stocks.service';
import {KeyValuePipe} from "@angular/common";

@Component({
    selector: 'app-stock-fundamentals',
    templateUrl: './stock-fundamentals.component.html',
    imports: [
        KeyValuePipe
    ],
    styleUrls: ['./stock-fundamentals.component.css']
})

export class StockFundamentalsComponent {

    public summary: StockDetails;
    public profile: StockProfile;

    constructor() {
    }

    get stock(): StockDetails {
        return this.summary;
    }

    @Input()
    set stock(stock: StockDetails) {
        this.profile = stock.profile;
        this.summary = stock
    }
}
