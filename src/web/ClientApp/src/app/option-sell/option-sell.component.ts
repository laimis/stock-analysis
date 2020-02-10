import { Component, OnInit } from '@angular/core';
import { StocksService, GetErrors } from '../services/stocks.service';
import { DatePipe, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-option-sell',
  templateUrl: './option-sell.component.html',
  styleUrls: ['./option-sell.component.css'],
  providers: [DatePipe]
})
export class OptionSellComponent implements OnInit {

  public errors : string[]

  public success: boolean

  public ticker : string
  public strikePrice: number
  public optionType: string
  public expirationDate: string
  public positionType: string
  public numberOfContracts: number
  public premium: number
  public filled: string;

  constructor(
    private service: StocksService,
    private route: ActivatedRoute,
    private router: Router,
    private datePipe: DatePipe,
    private location: Location) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

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
      filled: this.filled
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

  back() {
    this.location.back()
  }

  onTickerSelected(ticker:string) {
    this.ticker = ticker;
  }
}
