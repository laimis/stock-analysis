import {Component, Input, ChangeDetectionStrategy} from '@angular/core';
import {StockGaps} from '../../services/stocks.service';
import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, PercentPipe} from "@angular/common";

@Component({
    selector: 'app-gaps',
    templateUrl: './gaps.component.html',
    styleUrls: ['./gaps.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [
        NgClass,
        DatePipe,
        PercentPipe,
        CurrencyPipe,
        DecimalPipe
    ]
})
export class GapsComponent {

    error: string = null;

    @Input()
    gaps: StockGaps

}
