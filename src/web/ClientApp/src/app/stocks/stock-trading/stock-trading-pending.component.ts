import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { StocksService } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-pending',
  templateUrl: './stock-trading-pending.component.html',
  styleUrls: ['./stock-trading-pending.component.css']
})
export class StockTradingPendingComponent implements OnInit {

  constructor(
    private stockService: StocksService
  ) { }

	ngOnInit() {
  }

  @Input()
  pending: any

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
