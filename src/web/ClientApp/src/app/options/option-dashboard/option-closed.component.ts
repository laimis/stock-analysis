import { Component, Input } from '@angular/core';

@Component({
  selector: 'option-closed',
  templateUrl: './option-closed.component.html',
  styleUrls: ['./option-closed.component.css']
})

export class OptionClosedComponent {

  @Input()
  closedPositions: any;

	constructor(){}

	ngOnInit(): void {}
}
