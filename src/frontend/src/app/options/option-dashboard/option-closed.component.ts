import {Component, Input} from '@angular/core';
import {OwnedOption} from "../../services/option.service";

@Component({
    selector: 'app-option-closed',
    templateUrl: './option-closed.component.html',
    styleUrls: ['./option-closed.component.css'],
    standalone: false
})

export class OptionClosedComponent {

    @Input()
    closedOptions: OwnedOption[]

    constructor() {
    }
}
