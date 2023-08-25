import { Component, Output, EventEmitter, OnInit } from '@angular/core';
import { PendingStockPosition, StocksService } from 'src/app/services/stocks.service';
import { GetErrors } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-trading-pendingpositions',
  templateUrl: './stock-trading-pendingpositions.component.html',
  styleUrls: ['./stock-trading-pendingpositions.component.css']
})
export class StockTradingPendingPositionsComponent implements OnInit {
  errors: string[];
  
  constructor(
      private stockService:StocksService
      )
  { }

  positions: PendingStockPosition[] = [];
  loaded: boolean = false;

  @Output()
  pendingPositionClosed: EventEmitter<PendingStockPosition> = new EventEmitter<PendingStockPosition>()

  ngOnInit(): void {
    this.refreshPendingPositions()
  }

  refreshPendingPositions() {
    this.stockService.getPendingStockPositions().subscribe(
      (data) => {
        this.positions = data;
        this.loaded = true;
      }, err => {
        console.log(err)
        this.errors = GetErrors(err);
        this.loaded = true;
      }
    )
  }

  closePendingPosition(position: PendingStockPosition) {
    this.stockService.closePendingPosition(position.id).subscribe(
      (_) => {
        this.pendingPositionClosed.emit(position);
      },
      (error) => {
        console.log(error)
      }
    )
  }
}

