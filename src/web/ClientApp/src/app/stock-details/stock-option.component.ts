import { Component, OnInit, Input } from '@angular/core';
import { StocksService, GetErrors, OwnedOption } from '../services/stocks.service';
import { DatePipe, Location } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'stock-option',
  templateUrl: './stock-option.component.html',
  providers: [DatePipe]
})
export class StockOptionComponent implements OnInit {

  @Input()
  options      : OwnedOption[]
  @Input()
  ticker            : string

  errors : string[]
  success: boolean

  strikePrice       : number
  optionType        : string
  expirationDate    : string
  positionType      : string
  numberOfContracts : number
  premium           : number
  filled            : string
  notes             : string

  constructor(
    private service: StocksService,
    private router: Router,
    private datePipe: DatePipe) { }

  ngOnInit() {
    this.filled = Date()
    this.filled = this.datePipe.transform(this.filled, 'yyyy-MM-dd');
    this.positionType = 'buy'
  }

  record() {
    var opt = {
      ticker: this.ticker,
      strikePrice: this.strikePrice,
      optionType: this.optionType,
      expirationDate: this.expirationDate,
      numberOfContracts: this.numberOfContracts,
      premium: this.premium,
      filled: this.filled,
      notes: this.notes
    }

    if (this.positionType == 'buy') this.recordBuy(opt)
    if (this.positionType == 'sell') this.recordSell(opt)
  }

  recordBuy(opt: object) {
    this.service.buyOption(opt).subscribe( r => {
      this.navigateToOption(r.id)
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  recordSell(opt: object) {
    this.service.sellOption(opt).subscribe( r => {
      this.navigateToOption(r.id)
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  navigateToOption(id:string) {
    this.router.navigate(['/optiondetails', id])
  }

  onTickerSelected(ticker:string) {
    this.ticker = ticker;
  }
}
