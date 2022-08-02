import { Component, OnInit, Input } from '@angular/core';


@Component({
  selector: 'stock-trading-pending',
  templateUrl: './stock-trading-pending.component.html',
  styleUrls: ['./stock-trading-pending.component.css']
})
export class StockTradingPendingComponent implements OnInit {

	ngOnInit() {
  }

  @Input()
  pending: any
}
