import { Component, Input } from '@angular/core';

@Component({
  selector: 'option-open',
  templateUrl: './option-open.component.html',
  styleUrls: ['./option-open.component.css']
})

export class OptionOpenComponent {

  openOptions : any

  @Input()
  set statsContainer(container:any) {
    this.openOptions = container.openOptions
  }

	constructor(){}

	ngOnInit(): void {}
}
