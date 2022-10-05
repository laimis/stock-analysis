import { Component, OnInit, Input } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-past',
  templateUrl: './stock-trading-past.component.html',
  styleUrls: ['./stock-trading-past.component.css']
})
export class StockTradingPastComponent implements OnInit {

	ngOnInit() {
  }

  @Input()
  positions: PositionInstance[]
}
