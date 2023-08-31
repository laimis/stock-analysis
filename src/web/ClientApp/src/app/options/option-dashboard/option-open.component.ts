import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { OwnedOption } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-option-open',
  templateUrl: './option-open.component.html',
  styleUrls: ['./option-open.component.css']
})

export class OptionOpenComponent {

  private _openOptions : OwnedOption[] = []
  premiumSum: number;
  premiumCloseValue: number;

  @Input()
  set openOptions(value : OwnedOption[]) {
    if (value == null) {
      value = []
    }
    this._openOptions = value
    this.premiumSum = value.reduce((a, b) => a - b.premiumPaid + b.premiumReceived, 0)
    this.premiumCloseValue = value.reduce((a, b) => {
      if (b.boughtOrSold == 'Bought') {
        return a + b.detail.bid
      } else {
        return a - b.detail.ask
      }
    }, 0) * 100
  }
  get openOptions() : OwnedOption[] {
    return this._openOptions
  }

	constructor(private router: Router){}

  onTickerSelected(ticker:string) {
    this.router.navigateByUrl('/stocks/' + ticker)
  }

  intrinsicValue(option:OwnedOption) : number {
    if (option.optionType == 'CALL') {
      if (option.currentPrice > option.strikePrice) {
        return option.currentPrice - option.strikePrice
      }
      else {
        return 0
      }
    }

    if (option.optionType == 'PUT') {
      if (option.currentPrice < option.strikePrice) {
        return option.strikePrice - option.currentPrice
      }
      else {
        return 0
      }
    }

    console.log(option.optionType)
    return 0
  }

  extrinsicValue(option:OwnedOption) : number {
    return (option.detail.ask + option.detail.bid) / 2 - this.intrinsicValue(option)
  }
}
