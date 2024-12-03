import {Component, Input} from '@angular/core';

@Component({
    selector: 'app-option-stats',
    templateUrl: './option-stats.component.html',
    styleUrls: ['./option-stats.component.css'],
    standalone: false
})

export class OptionStatsComponent {

    @Input()
    stats: any;

    constructor() {
    }
}
