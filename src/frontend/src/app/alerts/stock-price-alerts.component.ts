import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StocksService, StockPriceAlert } from '../services/stocks.service';
import { GetErrors } from '../services/utils';
import { StockSearchComponent } from '../stocks/stock-search/stock-search.component';

@Component({
  selector: 'app-stock-price-alerts',
  templateUrl: './stock-price-alerts.component.html',
  styleUrls: ['./stock-price-alerts.component.css'],
  imports: [CommonModule, FormsModule, StockSearchComponent]
})
export class StockPriceAlertsComponent implements OnInit {
  private stocksService = inject(StocksService);

  alerts: StockPriceAlert[] = [];
  filteredAlerts: StockPriceAlert[] = [];
  errors: string[] = [];
  loading = false;
  showForm = false;
  filterState = 'all'; // 'all', 'active', 'triggered', 'disabled'

  // Form fields
  newAlert = {
    ticker: '',
    priceLevel: null as number | null,
    alertType: 'below',
    note: ''
  };

  editingAlert: StockPriceAlert | null = null;

  ngOnInit() {
    this.loadAlerts();
  }

  loadAlerts() {
    this.loading = true;
    this.errors = [];
    
    this.stocksService.getStockPriceAlerts().subscribe({
      next: (alerts) => {
        this.alerts = alerts;
        this.applyFilter();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  applyFilter() {
    if (this.filterState === 'all') {
      this.filteredAlerts = [...this.alerts];
    } else {
      this.filteredAlerts = this.alerts.filter(a => a.state === this.filterState);
    }
    
    // Sort by ticker, then by price level
    this.filteredAlerts.sort((a, b) => {
      const tickerCompare = a.ticker.localeCompare(b.ticker);
      if (tickerCompare !== 0) return tickerCompare;
      return a.priceLevel - b.priceLevel;
    });
  }

  onFilterChange() {
    this.applyFilter();
  }

  toggleForm() {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.resetForm();
    }
  }

  onTickerSelected(ticker: string) {
    this.newAlert.ticker = ticker;
  }

  resetForm() {
    this.newAlert = {
      ticker: '',
      priceLevel: null,
      alertType: 'below',
      note: ''
    };
    this.editingAlert = null;
  }

  createAlert() {
    if (!this.newAlert.ticker || !this.newAlert.priceLevel) {
      this.errors = ['Ticker and price level are required'];
      return;
    }

    this.loading = true;
    this.errors = [];

    this.stocksService.createStockPriceAlert(
      this.newAlert.ticker.toUpperCase(),
      this.newAlert.priceLevel,
      this.newAlert.alertType,
      this.newAlert.note
    ).subscribe({
      next: () => {
        this.loadAlerts();
        this.toggleForm();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  editAlert(alert: StockPriceAlert) {
    this.editingAlert = { ...alert };
    this.showForm = true;
    this.newAlert = {
      ticker: alert.ticker,
      priceLevel: alert.priceLevel,
      alertType: alert.alertType,
      note: alert.note
    };
  }

  updateAlert() {
    if (!this.editingAlert || !this.newAlert.priceLevel) {
      this.errors = ['Invalid alert or price level'];
      return;
    }

    this.loading = true;
    this.errors = [];

    this.stocksService.updateStockPriceAlert(
      this.editingAlert.alertId,
      this.newAlert.priceLevel,
      this.newAlert.alertType,
      this.newAlert.note,
      this.editingAlert.state
    ).subscribe({
      next: () => {
        this.loadAlerts();
        this.toggleForm();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  deleteAlert(alert: StockPriceAlert) {
    if (!confirm(`Delete alert for ${alert.ticker} at $${alert.priceLevel}?`)) {
      return;
    }

    this.loading = true;
    this.errors = [];

    this.stocksService.deleteStockPriceAlert(alert.alertId).subscribe({
      next: () => {
        this.loadAlerts();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  resetAlertState(alert: StockPriceAlert) {
    if (!confirm(`Reset alert for ${alert.ticker}?`)) {
      return;
    }

    this.loading = true;
    this.errors = [];

    this.stocksService.updateStockPriceAlert(
      alert.alertId,
      alert.priceLevel,
      alert.alertType,
      alert.note,
      'active'
    ).subscribe({
      next: () => {
        this.loadAlerts();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
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
}
