import { Component, Output, EventEmitter } from '@angular/core';
import { Observable } from 'rxjs';
import { brokerageordercommand, StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-brokerage-neworder',
  templateUrl: './neworder.component.html',
  styleUrls: ['./neworder.component.css']
})
export class BrokerageNewOrderComponent {

  brokerageOrderDuration : string
  brokerageOrderType : string
  numberOfShares : number | null = null
  price : number | null = null
  ticker : string

  constructor(
    private stockService: StocksService
  ) { }

  @Output()
  brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()

  updateTotals() {
  }

  reset() {
    this.numberOfShares = null
    this.price = null
    this.ticker = null
    this.brokerageOrderDuration = null
    this.brokerageOrderType = null
  }

  onTickerSelected(ticker: string) {
    this.ticker = ticker
    this.stockService.getStockPrice(ticker).subscribe(
      prices => {
        this.price = prices
      }
    )
  }

  brokerageBuy() {
    this.execute(this.stockService.brokerageBuy)
  }

  brokerageSell() {
    this.execute(this.stockService.brokerageSell)
  }

  execute(fn: (cmd: brokerageordercommand) => Observable<string>) {
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
        console.error(err)
        alert('brokerage failed') 
      }
    )
  }
}
