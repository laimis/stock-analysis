import {Component, Input} from '@angular/core';
import {StockDetails, StockProfile} from '../../services/stocks.service';

@Component({
    selector: 'app-stock-fundamentals',
    templateUrl: './stock-fundamentals.component.html',
    styleUrls: ['./stock-fundamentals.component.css'],
    standalone: false
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
