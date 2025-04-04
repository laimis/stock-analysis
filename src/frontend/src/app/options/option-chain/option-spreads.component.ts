import {Component, Input} from '@angular/core';
import {OptionSpread} from "../../services/option.service";

@Component({
    selector: 'app-option-spreads',
    templateUrl: './option-spreads.component.html',
    styleUrls: ['./option-spreads.component.css'],
    standalone: false
})
export class OptionSpreadsComponent {

    @Input()
    spreads: OptionSpread[] = [];
}
