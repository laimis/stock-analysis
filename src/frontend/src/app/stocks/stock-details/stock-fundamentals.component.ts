import {Component, Input} from '@angular/core';
import {StockDetails, StockProfile} from '../../services/stocks.service';
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, PercentPipe} from "@angular/common";
import { MarketCapPipe } from 'src/app/services/marketcap.filter';

@Component({
    selector: 'app-stock-fundamentals',
    templateUrl: './stock-fundamentals.component.html',
    imports: [
        DecimalPipe,
        CurrencyPipe,
        PercentPipe,
        MarketCapPipe,
        DatePipe,
        NgClass
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

    getNumberClass(value: number, lowEnd:number, highEnd:number): string {
        if (!value) return '';
        if (value < lowEnd) return 'metric-low';
        if (value > highEnd) return 'metric-high';
        return 'metric-normal';
    }
}
