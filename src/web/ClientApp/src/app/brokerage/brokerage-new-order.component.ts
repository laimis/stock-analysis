import {Component, Output, EventEmitter, Input} from '@angular/core';
import { Observable } from 'rxjs';
import { brokerageordercommand, KeyValuePair, StockQuote, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from '../services/utils';
import {BrokerageOrderDuration, BrokerageOrderType} from "../services/brokerage.service";


@Component({
  selector: 'app-brokerage-new-order',
  templateUrl: './brokerage-new-order.component.html',
  styleUrls: ['./brokerage-new-order.component.css']
})
export class BrokerageNewOrderComponent {

  brokerageOrderDuration : string
  brokerageOrderType : string
  numberOfShares : number | null = null
  price : number | null = null
  selectedTicker : string
  quote : StockQuote
  total: number | null = null
  errorMessage: string | null = null;

  submittedOrder = false
  submittingOrder = false

  private marketOrderTypes: KeyValuePair[] = [
    { key: BrokerageOrderDuration.Day, value: 'Day' },
    { key: BrokerageOrderDuration.Gtc, value: 'GTC' }
  ]
  private nonMarketOrderTypes: KeyValuePair[] = [
    { key: BrokerageOrderDuration.Day, value: 'Day' },
    { key: BrokerageOrderDuration.Gtc, value: 'GTC' },
    { key: BrokerageOrderDuration.DayPlus, value: 'Day+AH' },
    { key: BrokerageOrderDuration.GtcPlus, value: 'GTC+AH' }
  ]

  orderDurations: KeyValuePair[]

  constructor(
    private stockService: StocksService
  )
  {
    this.brokerageOrderType = BrokerageOrderType.Limit
    this.brokerageOrderTypeChanged()
  }

  @Output()
  brokerageOrderEntered: EventEmitter<string> = new EventEmitter<string>()

  @Input()
  set ticker(value: string) {
    this.selectTicker(value)
  }

  numberOfSharesChanged() {
    if (this.numberOfShares && this.price) {
      this.total = this.numberOfShares * this.price
    } else {
      this.total = null
    }
  }

  brokerageOrderTypeChanged() {
    if (this.brokerageOrderType === BrokerageOrderType.Market) {
      this.orderDurations = this.marketOrderTypes
      this.brokerageOrderDuration = BrokerageOrderDuration.Gtc
    }
    else {
      this.orderDurations = this.nonMarketOrderTypes
      this.brokerageOrderDuration = BrokerageOrderDuration.GtcPlus
    }
  }

  reset() {
    this.numberOfShares = null
    this.errorMessage = null
  }

  onTickerSelected(ticker: string) {
    this.selectTicker(ticker)
  }

  selectTicker(ticker: string) {
    this.selectedTicker = ticker
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
    this.submittingOrder = true
    var cmd : brokerageordercommand = {
      ticker: this.selectedTicker,
      numberOfShares: this.numberOfShares,
      price: this.price,
      type: this.brokerageOrderType,
      duration: this.brokerageOrderDuration
    }

    fn(cmd).subscribe(
      _ => {
        this.submittingOrder = false
        this.submittedOrder = true
        this.reset()
        this.brokerageOrderEntered.emit(this.selectedTicker)
    },
      err => {
        this.errorMessage = GetErrors(err)[0]
        console.error(err)
      }
    )
  }
}
