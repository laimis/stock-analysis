import {Component, Input} from '@angular/core';
import {OwnedOption} from "../../services/option.service";
import {StockLinkComponent} from "../../shared/stocks/stock-link.component";
import {CurrencyPipe, DatePipe, PercentPipe} from "@angular/common";
import {RouterLink} from "@angular/router";

@Component({
    selector: 'app-option-closed',
    templateUrl: './option-closed.component.html',
    styleUrls: ['./option-closed.component.css'],
    imports: [
        StockLinkComponent,
        CurrencyPipe,
        DatePipe,
        PercentPipe,
        RouterLink
    ]
})

export class OptionClosedComponent {

    @Input()
    closedOptions: OwnedOption[]

    constructor() {
    }
}
