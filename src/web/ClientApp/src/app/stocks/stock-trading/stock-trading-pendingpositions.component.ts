import { Component, Output, EventEmitter, OnInit } from '@angular/core';
import { charts_getTradingViewLink } from 'src/app/services/links.service';
import { PendingStockPosition, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-trading-pendingpositions',
  templateUrl: './stock-trading-pendingpositions.component.html',
  styleUrls: ['./stock-trading-pendingpositions.component.css']
})
export class StockTradingPendingPositionsComponent implements OnInit {
  
  constructor(
      private stockService:StocksService
      )
  { }

  positions: PendingStockPosition[] = [];

  
  @Output()
  stockPurchased: EventEmitter<stocktransactioncommand> = new EventEmitter<stocktransactioncommand>()

  ngOnInit(): void {
    this.refreshPendingPositions()
  }

  refreshPendingPositions() {
    this.stockService.getPendingStockPositions().subscribe(
      (data) => {
        this.positions = data;
      }
    )
  }

  closePendingPosition(position: PendingStockPosition) {
    this.stockService.closePendingPosition(position.id).subscribe(
      (_) => {
        this.refreshPendingPositions();
      },
      (error) => {
        console.log(error)
      }
    )
  }

  getTradingViewLink(ticker: string) {
    return charts_getTradingViewLink(ticker)
  }
}

