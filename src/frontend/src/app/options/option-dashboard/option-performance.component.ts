import {Component, Input} from '@angular/core';

@Component({
    selector: 'app-option-performance',
    templateUrl: './option-performance.component.html',
    styleUrls: ['./option-performance.component.css'],
    standalone: false
})

export class OptionPerformanceComponent {

    @Input()
    overallStats: any

    @Input()
    buyStats: any

    @Input()
    sellStats: any
}
