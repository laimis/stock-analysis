import {Component, Input} from '@angular/core';
import {StockGaps} from '../../services/stocks.service';
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, PercentPipe} from "@angular/common";

@Component({
    selector: 'app-gaps',
    templateUrl: './gaps.component.html',
    styleUrls: ['./gaps.component.css'],
    imports: [
        NgClass,
        DatePipe,
        PercentPipe,
        CurrencyPipe,
        DecimalPipe
    ],
    standalone: true
})
export class GapsComponent {

    error: string = null;

    @Input()
    gaps: StockGaps

}
