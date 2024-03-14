import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-error-display',
    standalone: true,
  templateUrl: './error-display.component.html',
  styleUrls: ['./error-display.component.css']
})
export class ErrorDisplayComponent {

  @Input() errors: string[];

}
