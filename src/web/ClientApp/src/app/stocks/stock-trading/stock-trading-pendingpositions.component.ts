import { DatePipe } from '@angular/common';
import { Component, Output, EventEmitter, OnInit } from '@angular/core';
import { PendingStockPosition, StocksService, stocktransactioncommand } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-trading-pendingpositions',
  templateUrl: './stock-trading-pendingpositions.component.html',
  styleUrls: ['./stock-trading-pendingpositions.component.css'],
  providers: [DatePipe]
})
export class StockTradingPendingPositionsComponent implements OnInit {
  
  constructor(
      private stockService:StocksService,
      private datePipe: DatePipe
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
        console.log(data)
      }
    )
  }
}

