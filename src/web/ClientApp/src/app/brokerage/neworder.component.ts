import { Component, Output, EventEmitter, Input } from '@angular/core';
import { Observable } from 'rxjs';
import { brokerageordercommand, PositionInstance, StockQuote, StocksService } from 'src/app/services/stocks.service';


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

  constructor(
    private stockService: StocksService
  ) { }

  @Output()
  brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()

  @Input()
  positions: PositionInstance[] = []

  reset() {
    this.numberOfShares = null
    this.price = null
    this.ticker = null
    this.brokerageOrderDuration = null
    this.brokerageOrderType = null
  }

  onTickerSelected(ticker: string) {
    this.ticker = ticker
    this.stockService.getStockQuote(ticker).subscribe(
      prices => {
        this.quote = prices
        console.log(this.quote)
        this.price = prices.mark

        var position = this.positions.find(p => p.ticker === ticker)
        if (position) {
          this.numberOfShares = position.numberOfShares
        }
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
