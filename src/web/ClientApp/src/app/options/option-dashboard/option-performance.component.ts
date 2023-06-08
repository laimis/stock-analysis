import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-option-performance',
  templateUrl: './option-performance.component.html',
  styleUrls: ['./option-performance.component.css']
})

export class OptionPerformanceComponent {

  overall: any
  buy: any
  sell: any

  @Input()
  set statsContainer(container:any) {
    this.overall = container.overall
    this.buy = container.buy
    this.sell = container.sell
  }

	constructor(){}

	ngOnInit(): void {}
}
