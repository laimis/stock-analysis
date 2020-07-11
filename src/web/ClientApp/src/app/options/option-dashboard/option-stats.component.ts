import { Component, Input } from '@angular/core';

@Component({
  selector: 'option-stats',
  templateUrl: './option-stats.component.html',
  styleUrls: ['./option-stats.component.css']
})

export class OptionStatsComponent {

  @Input()
  stats: any;

	constructor(){}

	ngOnInit(): void {}
}
