import { DecimalPipe, NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TickerFundamentals } from 'src/app/services/stocks.service';
import { StockLinkAndTradingviewLinkComponent } from "../../stocks/stock-link-and-tradingview-link.component";
import { MarketCapPipe } from 'src/app/services/marketcap.filter';

@Component({
  selector: 'app-fundamentals',
  imports: [
    NgIf,
    NgClass,
    DecimalPipe,
    MarketCapPipe,
    StockLinkAndTradingviewLinkComponent
],
  templateUrl: './fundamentals.component.html',
  styleUrl: './fundamentals.component.css'
})
export class FundamentalsComponent {
  private _data: TickerFundamentals[] = [];
  sortedData: TickerFundamentals[] = [];
  sortColumn: string = 'ticker';
  sortDirection: number = 1;
  
  @Input()
  set data(value: TickerFundamentals[]) {
    if (!value) return;
    this._data = value;
    this.sortBy('ticker'); // Default sort by ticker
  }
  
  get data() {
    return this._data;
  }

  sortBy(column: string) {
    if (this.sortColumn === column) {
      // Reverse direction if same column clicked again
      this.sortDirection = this.sortDirection * -1;
    } else {
      this.sortColumn = column;
      this.sortDirection = 1;
    }
    
    this.sortedData = [...this._data].sort((a, b) => {
      // Handle null/undefined values
      let aVal = a.fundamentals[column] !== undefined && a.fundamentals[column] !== null ? a.fundamentals[column] : -Infinity;
      let bVal = b.fundamentals[column] !== undefined && b.fundamentals[column] !== null ? b.fundamentals[column] : -Infinity;
      
      // try to parse as number if possible
      if (typeof aVal === 'string' && !isNaN(parseFloat(aVal))) {
        aVal = parseFloat(aVal);
      }
      if (typeof bVal === 'string' && !isNaN(parseFloat(bVal))) {
        bVal = parseFloat(bVal);
      }

      if (this.sortDirection === 1) {
        return aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      } else {
        return bVal < aVal ? -1 : bVal > aVal ? 1 : 0;
      }
    });
  }

  getNumberClass(value: number, lowEnd: number, highEnd: number): string {
    if (!value || isNaN(value)) return '';
    
    if (value < lowEnd) return 'metric-low';
    if (value > highEnd) return 'metric-high';
    return 'metric-normal';
  }


}
