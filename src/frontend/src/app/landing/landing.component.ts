import {Component, ChangeDetectionStrategy} from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-landing',
    templateUrl: './landing.component.html',
    styleUrls: ['./landing.component.css'],
    imports: [RouterLink],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: true
})
export class LandingComponent {

    success: boolean = false
    errors: string[]

    constructor() {
    }
}
