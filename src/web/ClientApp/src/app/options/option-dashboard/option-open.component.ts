import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-option-open',
  templateUrl: './option-open.component.html',
  styleUrls: ['./option-open.component.css']
})

export class OptionOpenComponent {

  openOptions : any

  @Input()
  set statsContainer(container:any) {
    this.openOptions = container.openOptions
  }

	constructor(private router: Router){}

  ngOnInit(): void {}

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }
}
