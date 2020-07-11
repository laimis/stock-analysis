import { Component, Input } from '@angular/core';

@Component({
  selector: 'option-open',
  templateUrl: './option-open.component.html',
  styleUrls: ['./option-open.component.css']
})

export class OptionOpenComponent {

  @Input()
  openPositions: any;

	constructor(){}

	ngOnInit(): void {}
}
