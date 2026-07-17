import {Component, Input, ChangeDetectionStrategy} from '@angular/core';
import {OptionSpread} from "../../services/option.service";
import { CurrencyPipe } from '@angular/common';

@Component({
    selector: 'app-option-spreads',
    templateUrl: './option-spreads.component.html',
    styleUrls: ['./option-spreads.component.css'],
    imports: [CurrencyPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true
})
export class OptionSpreadsComponent {

    @Input()
    spreads: OptionSpread[] = [];
}
