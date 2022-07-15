import { Component, OnInit, Input } from '@angular/core';
import { StockTradingGridEntry } from 'src/app/services/stocks.service';
import { StockGridComponent } from '../stock-dashboard/stock-grid.component';


@Component({
  selector: 'stock-trading-review',
  templateUrl: './stock-trading-review.component.html',
  styleUrls: ['./stock-trading-review.component.css']
})
export class StockTradingReviewComponent implements OnInit {
  updateCurrentPosition() {
    this.currentPosition = this._positions[this._index]
  }
  private _positions: StockTradingGridEntry[];

	ngOnInit() {
  }

  @Input()
  set positions(positions:StockTradingGridEntry[]) {
    this._positions = positions
    this._index = 0
    this.updateCurrentPosition()
  }

  private _index: number = 0
  currentPosition: StockTradingGridEntry

  next() {
    this._index++
    if (this._index >= this._positions.length) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

  previous() {
    this._index--
    if (this._index < 0) {
      this._index = 0
    }
    this.updateCurrentPosition()
  }

}
