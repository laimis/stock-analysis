import { Component, OnInit, Input } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent implements OnInit {

	ngOnInit() {
  }

  @Input()
  positions: PositionInstance[]
}
