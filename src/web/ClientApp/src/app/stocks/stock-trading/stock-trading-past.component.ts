import { Component, OnInit, Input } from '@angular/core';


@Component({
  selector: 'stock-past',
  templateUrl: './stock-trading-past.component.html',
  styleUrls: ['./stock-trading-past.component.css']
})
export class StockTradingPastComponent implements OnInit {

	ngOnInit() {
  }

  @Input()
  past: any
}
