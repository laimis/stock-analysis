import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { StockPriceAlert } from '../services/stocks.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';

@Component({
  selector: 'app-price-alert-list',
  templateUrl: './price-alert-list.component.html',
  styleUrls: ['./price-alert-list.component.css'],
  imports: [CommonModule, DatePipe, DecimalPipe, StockLinkAndTradingviewLinkComponent]
})
export class PriceAlertListComponent {
  @Input() alerts: StockPriceAlert[] = [];
  @Input() loading: boolean = false;
  @Input() showTickerColumn: boolean = true;  // Show ticker column (hide on stock-details page)
  @Input() emptyMessage: string = 'No alerts found. Create your first alert to get started!';
  
  @Output() edit = new EventEmitter<StockPriceAlert>();
  @Output() delete = new EventEmitter<StockPriceAlert>();
  @Output() resetAlert = new EventEmitter<StockPriceAlert>();

  onEdit(alert: StockPriceAlert) {
    this.edit.emit(alert);
  }

  onDelete(alert: StockPriceAlert) {
    if (confirm(`Delete alert for ${alert.ticker} at $${alert.priceLevel}?`)) {
      this.delete.emit(alert);
    }
  }

  onReset(alert: StockPriceAlert) {
    if (confirm(`Reset alert for ${alert.ticker} at $${alert.priceLevel}?`)) {
      this.resetAlert.emit(alert);
    }
  }

  getStateClass(state: string): string {
    switch (state) {
      case 'active': return 'badge bg-success';
      case 'triggered': return 'badge bg-warning text-dark';
      case 'disabled': return 'badge bg-secondary';
      default: return 'badge bg-secondary';
    }
  }

  getAlertTypeDisplay(alertType: string): string {
    return alertType === 'above' ? '↑ Above' : '↓ Below';
  }

  getAlertTypeClass(alertType: string): string {
    return alertType === 'above' ? 'text-success' : 'text-danger';
  }
}
