import {Component, Input} from '@angular/core';
import {OptionSpread} from "../../services/option.service";
import { CurrencyPipe } from '@angular/common';

@Component({
    selector: 'app-option-spreads',
    templateUrl: './option-spreads.component.html',
    styleUrls: ['./option-spreads.component.css'],
    imports: [CurrencyPipe],
    standalone: true
})
export class OptionSpreadsComponent {

    @Input()
    spreads: OptionSpread[] = [];
}
