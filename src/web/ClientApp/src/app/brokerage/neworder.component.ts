import { Component, Output, EventEmitter } from '@angular/core';
import { Observable } from 'rxjs';
import { brokerageordercommand, StockQuote, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from '../services/utils';


@Component({
  selector: 'app-brokerage-neworder',
  templateUrl: './neworder.component.html',
  styleUrls: ['./neworder.component.css']
})
export class BrokerageNewOrderComponent {

  brokerageOrderDuration : string = 'GtcPlus'
  brokerageOrderType : string = 'Limit'
  numberOfShares : number | null = null
  price : number | null = null
  ticker : string
  quote : StockQuote
  total: number | null = null
  errorMessage: string | null = null;

  constructor(
    private stockService: StocksService
  ) { }

  @Output()
  brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()

  numberOfSharesChanged() {
    if (this.numberOfShares && this.price) {
      this.total = this.numberOfShares * this.price
    } else {
      this.total = null
    }
  }

  reset() {
    this.numberOfShares = null
    this.price = null
    this.errorMessage = null
  }

  onTickerSelected(ticker: string) {
    this.ticker = ticker
    this.stockService.getStockQuote(ticker).subscribe(
      prices => {
        this.quote = prices
        this.price = prices.mark

        this.stockService.getStockOwnership(ticker).subscribe(
          ownership => {
            if (ownership.currentPosition) {
              this.numberOfShares = ownership.currentPosition.numberOfShares
            }
          }
        )
      }
    )
  }

  brokerageBuy() {
    this.execute(cmd => this.stockService.brokerageBuy(cmd))
  }

  brokerageSell() {
    this.execute(cmd => this.stockService.brokerageSell(cmd))
  }

  execute(fn: (cmd: brokerageordercommand) => Observable<string>) {
    this.errorMessage = null
    var cmd : brokerageordercommand = {
      ticker: this.ticker,
      numberOfShares: this.numberOfShares,
      price: this.price,
      type: this.brokerageOrderType,
      duration: this.brokerageOrderDuration
    }

    fn(cmd).subscribe(
      _ => { 
        this.reset()
        this.brokerageOrderEntered.emit("entered")
    },
      err => { 
        this.errorMessage = GetErrors(err)[0]
        console.error(err)
      }
    )
  }
}
