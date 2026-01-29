import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockPriceAlert, StocksService } from '../services/stocks.service';
import { StockSearchComponent } from '../stocks/stock-search/stock-search.component';

@Component({
  selector: 'app-price-alert-form',
  templateUrl: './price-alert-form.component.html',
  styleUrls: ['./price-alert-form.component.css'],
  imports: [CommonModule, FormsModule, StockSearchComponent, DecimalPipe]
})
export class PriceAlertFormComponent implements OnChanges {
  private stocksService = inject(StocksService);

  @Input() editingAlert: StockPriceAlert | null = null;
  @Input() presetTicker?: string;  // Pre-fill ticker (e.g., from stock-details page)
  @Input() showTickerField: boolean = true;  // Hide ticker field if preset
  @Input() loading: boolean = false;
  @Input() errors: string[] = [];
  
  @Output() save = new EventEmitter<{ ticker?: string; priceLevel: number; alertType: string; note: string }>();
  @Output() cancelled = new EventEmitter<void>();

  alertData = {
    ticker: '',
    priceLevel: null as number | null,
    alertType: 'below',
    note: ''
  };

  currentPrice: number | null = null;
  priceLoading: boolean = false;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['editingAlert'] && this.editingAlert) {
      this.alertData = {
        ticker: this.editingAlert.ticker,
        priceLevel: this.editingAlert.priceLevel,
        alertType: this.editingAlert.alertType,
        note: this.editingAlert.note
      };
      this.fetchCurrentPrice(this.editingAlert.ticker);
    }

    if (changes['presetTicker'] && this.presetTicker) {
      this.alertData.ticker = this.presetTicker;
      this.fetchCurrentPrice(this.presetTicker);
    }
  }

  onTickerSelected(ticker: string) {
    this.alertData.ticker = ticker;
    this.fetchCurrentPrice(ticker);
  }

  fetchCurrentPrice(ticker: string) {
    if (!ticker) return;
    
    this.priceLoading = true;
    this.currentPrice = null;

    this.stocksService.getStockQuote(ticker).subscribe({
      next: (quote) => {
        this.currentPrice = quote.price || quote.mark || quote.lastPrice;
        this.priceLoading = false;
      },
      error: (error) => {
        console.error('Error fetching price:', error);
        this.priceLoading = false;
      }
    });
  }

  setPriceLevelPercentage(percentage: number) {
    if (this.currentPrice !== null) {
      const multiplier = 1 + (percentage / 100);
      this.alertData.priceLevel = Math.round(this.currentPrice * multiplier * 100) / 100;
    }
  }

  get pricePercentageDiff(): number | null {
    if (this.currentPrice !== null && this.alertData.priceLevel !== null) {
      return ((this.alertData.priceLevel - this.currentPrice) / this.currentPrice) * 100;
    }
    return null;
  }

  onSubmit() {
    if (!this.alertData.priceLevel) {
      return;
    }

    const ticker = this.showTickerField 
      ? (this.alertData.ticker.trim() || undefined)
      : this.presetTicker;

    if (!ticker) {
      return;
    }

    this.save.emit({
      ticker,
      priceLevel: this.alertData.priceLevel,
      alertType: this.alertData.alertType,
      note: this.alertData.note
    });
  }

  onCancel() {
    this.reset();
    this.cancelled.emit();
  }

  reset() {
    this.alertData = {
      ticker: this.presetTicker || '',
      priceLevel: null,
      alertType: 'below',
      note: ''
    };
    this.currentPrice = null;
    this.priceLoading = false;
  }
}
