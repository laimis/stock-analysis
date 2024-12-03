import {Component} from '@angular/core';

@Component({
    selector: 'app-landing',
    templateUrl: './landing.component.html',
    styleUrls: ['./landing.component.css'],
    standalone: false
})
export class LandingComponent {

    success: Boolean = false
    errors: string[]

    constructor() {
    }
}
