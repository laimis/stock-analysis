import {Component, ChangeDetectionStrategy} from '@angular/core';

@Component({
    standalone: true,
    selector: 'app-loading',
    templateUrl: './loading.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './loading.component.css'
})
export class LoadingComponent {

}
