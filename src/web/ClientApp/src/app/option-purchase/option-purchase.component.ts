import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { Location, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-purchase',
  templateUrl: './option-purchase.component.html',
  styleUrls: ['./option-purchase.component.css'],
  providers: [DatePipe]
})
export class OptionPurchaseComponent implements OnInit {

  public ticker: string
  public amount: Number
  public strikePrice: Number
  public premium: Number
  public date: String
  public optionType: String
  public buyOrSell: string

  public purchased: Boolean

  constructor(
    private service: StocksService,
    private location: Location,
    private route: ActivatedRoute,
    private datePipe: DatePipe) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.date = Date()
    this.date = this.datePipe.transform(this.date, 'yyyy-MM-dd');
  }

  submitPurchase() {

    this.purchased = false;

    this.service.openOption(this.toObject());
    // .subscribe(() => {
    // 	this.purchased = true;
    // 	this.clearValues()
    // })
  }

  submitSell() {

    this.purchased = false;

    this.service.sell(this.toObject()).subscribe(() => {
      this.purchased = true;
      this.clearValues()
    })
  }

  toObject() {
    return {
      ticker: this.ticker,
      premium: this.premium,
      amount: this.amount,
      strikePrice: this.strikePrice,
      optionType: this.optionType,
      date: this.date,
      buyOrSell: this.buyOrSell
    }
  }

  clearValues() {
    this.ticker = null
    this.premium = null
    this.amount = null
    this.strikePrice = null
    this.optionType = null
    this.date = null
    this.buyOrSell = null
  }

  back() {
    this.location.back();
  }
}
