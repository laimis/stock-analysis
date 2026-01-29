import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StocksService, StockPriceAlert } from '../services/stocks.service';
import { GetErrors } from '../services/utils';
import { PriceAlertFormComponent } from './price-alert-form.component';
import { PriceAlertListComponent } from './price-alert-list.component';

@Component({
  selector: 'app-stock-price-alerts',
  templateUrl: './stock-price-alerts.component.html',
  styleUrls: ['./stock-price-alerts.component.css'],
  imports: [CommonModule, FormsModule, PriceAlertFormComponent, PriceAlertListComponent]
})
export class StockPriceAlertsComponent implements OnInit {
  private stocksService = inject(StocksService);

  alerts: StockPriceAlert[] = [];
  filteredAlerts: StockPriceAlert[] = [];
  errors: string[] = [];
  loading = false;
  showForm = false;
  filterState = 'all'; // 'all', 'active', 'triggered', 'disabled'
  filterTicker = ''; // Search by ticker

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
    let filtered = [...this.alerts];
    
    // Filter by state
    if (this.filterState !== 'all') {
      filtered = filtered.filter(a => a.state === this.filterState);
    }
    
    // Filter by ticker (case-insensitive)
    if (this.filterTicker && this.filterTicker.trim()) {
      const searchTerm = this.filterTicker.trim().toLowerCase();
      filtered = filtered.filter(a => a.ticker.toLowerCase().includes(searchTerm));
    }
    
    // Sort by ticker, then by price level
    this.filteredAlerts = filtered.sort((a, b) => {
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

  resetForm() {
    this.editingAlert = null;
    this.errors = [];
  }

  onPriceAlertSave(data: { ticker?: string; priceLevel: number; alertType: string; note: string }) {
    this.loading = true;
    this.errors = [];

    if (this.editingAlert) {
      // Update existing alert
      this.stocksService.updateStockPriceAlert(
        this.editingAlert.alertId,
        data.priceLevel,
        data.alertType,
        data.note,
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
    } else {
      // Create new alert
      if (!data.ticker) {
        this.errors = ['Ticker is required'];
        this.loading = false;
        return;
      }

      this.stocksService.createStockPriceAlert(
        data.ticker.toUpperCase(),
        data.priceLevel,
        data.alertType,
        data.note
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
  }

  editAlert(alert: StockPriceAlert) {
    this.editingAlert = alert;
    this.showForm = true;
  }

  deleteAlert(alert: StockPriceAlert) {
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
    this.loading = true;
    this.errors = [];

    this.stocksService.resetStockPriceAlert(alert.alertId).subscribe({
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
}
