import {Component, Input, ChangeDetectionStrategy} from '@angular/core';

@Component({
    selector: 'app-error-display',
    standalone: true,
    templateUrl: './error-display.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./error-display.component.css']
})
export class ErrorDisplayComponent {

    @Input() errors: string[];

}
