import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { OwnedOption } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-option-open',
  templateUrl: './option-open.component.html',
  styleUrls: ['./option-open.component.css']
})

export class OptionOpenComponent {

  @Input()
  openOptions : OwnedOption[]

	constructor(private router: Router){}

  ngOnInit(): void {}

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }
}
