import {Component, Input} from '@angular/core';
import {CurrencyPipe, DecimalPipe, PercentPipe} from "@angular/common";

@Component({
    selector: 'app-option-stats',
    templateUrl: './option-stats.component.html',
    imports: [
        PercentPipe,
        CurrencyPipe,
        DecimalPipe
    ],
    styleUrls: ['./option-stats.component.css']
})

export class OptionStatsComponent {

    @Input()
    stats: any;

    constructor() {
    }
}
