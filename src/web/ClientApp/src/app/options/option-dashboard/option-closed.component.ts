import { Component, Input } from '@angular/core';
import { OwnedOption } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-option-closed',
  templateUrl: './option-closed.component.html',
  styleUrls: ['./option-closed.component.css']
})

export class OptionClosedComponent {

  @Input()
  closedOptions: OwnedOption[]

	constructor(){}
}
