import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'brokerage-orders',
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class StockTradingPendingComponent implements OnInit {

  constructor(
    private stockService: StocksService
  ) { }

	ngOnInit() {
  }

  @Input()
  orders: any

  @Output()
  orderCancelled: EventEmitter<string> = new EventEmitter<string>()

  cancelOrder(orderId: string) {
    this.stockService.brokerageCancelOrder(orderId).subscribe(() => {
      this.orderCancelled.emit("cancelled")
    }, (err) => {
      console.log(err)
    }
    )
  }
}
