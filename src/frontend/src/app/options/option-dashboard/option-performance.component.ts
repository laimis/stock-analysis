import {Component, Input} from '@angular/core';
import {OptionStatsComponent} from "./option-stats.component";

@Component({
    selector: 'app-option-performance',
    templateUrl: './option-performance.component.html',
    styleUrls: ['./option-performance.component.css'],
    imports: [
        OptionStatsComponent
    ]
})

export class OptionPerformanceComponent {

    @Input()
    overallStats: any

    @Input()
    buyStats: any

    @Input()
    sellStats: any
}
