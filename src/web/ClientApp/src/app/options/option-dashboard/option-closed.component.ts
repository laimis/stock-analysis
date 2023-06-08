import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-option-closed',
  templateUrl: './option-closed.component.html',
  styleUrls: ['./option-closed.component.css']
})

export class OptionClosedComponent {

  options:any

  @Input()
  set statsContainer(container:any) {
    this.options = container.closedOptions
  }

	constructor(){}

	ngOnInit(): void {}
}
