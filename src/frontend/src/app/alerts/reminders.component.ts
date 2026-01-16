import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StocksService, Reminder } from '../services/stocks.service';
import { GetErrors } from '../services/utils';
import { StockLinkAndTradingviewLinkComponent } from "../shared/stocks/stock-link-and-tradingview-link.component";

@Component({
  selector: 'app-reminders',
  templateUrl: './reminders.component.html',
  styleUrls: ['./reminders.component.css'],
  imports: [CommonModule, FormsModule, StockLinkAndTradingviewLinkComponent]
})
export class RemindersComponent implements OnInit {
  private stocksService = inject(StocksService);

  reminders: Reminder[] = [];
  filteredReminders: Reminder[] = [];
  errors: string[] = [];
  loading = false;
  showForm = false;
  filterState = 'all'; // 'all', 'pending', 'sent', 'dismissed'

  // Form fields
  newReminder = {
    date: '',
    message: '',
    ticker: ''
  };

  editingReminder: Reminder | null = null;
  today = new Date().toISOString().split('T')[0]; // For min date validation

  ngOnInit() {
    this.loadReminders();
  }

  loadReminders() {
    this.loading = true;
    this.errors = [];
    
    this.stocksService.getReminders().subscribe({
      next: (reminders) => {
        this.reminders = reminders;
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
    let filtered = [...this.reminders];
    
    // Filter by state
    if (this.filterState !== 'all') {
      filtered = filtered.filter(r => r.state === this.filterState);
    }
    
    // Sort by date ascending (soonest first)
    this.filteredReminders = filtered.sort((a, b) => {
      return new Date(a.date).getTime() - new Date(b.date).getTime();
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
    this.newReminder = {
      date: '',
      message: '',
      ticker: ''
    };
    this.editingReminder = null;
    this.errors = [];
  }

  createReminder() {
    if (!this.newReminder.date || !this.newReminder.message.trim()) {
      this.errors = ['Date and message are required'];
      return;
    }

    this.loading = true;
    this.errors = [];

    const ticker = this.newReminder.ticker.trim() || undefined;

    this.stocksService.createReminder(
      this.newReminder.date,
      this.newReminder.message,
      ticker
    ).subscribe({
      next: () => {
        this.loadReminders();
        this.toggleForm();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  editReminder(reminder: Reminder) {
    this.editingReminder = reminder;
    this.newReminder = {
      date: new Date(reminder.date).toISOString().split('T')[0],
      message: reminder.message,
      ticker: reminder.ticker || ''
    };
    this.showForm = true;
  }

  updateReminder() {
    if (!this.editingReminder) return;

    if (!this.newReminder.date || !this.newReminder.message.trim()) {
      this.errors = ['Date and message are required'];
      return;
    }

    this.loading = true;
    this.errors = [];

    const ticker = this.newReminder.ticker.trim() || undefined;

    this.stocksService.updateReminder(
      this.editingReminder.reminderId,
      this.newReminder.date,
      this.newReminder.message,
      ticker,
      this.editingReminder.state
    ).subscribe({
      next: () => {
        this.loadReminders();
        this.toggleForm();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  dismissReminder(reminder: Reminder) {
    if (!confirm('Dismiss this reminder?')) return;

    this.loading = true;
    this.errors = [];

    this.stocksService.updateReminder(
      reminder.reminderId,
      new Date(reminder.date).toISOString(),
      reminder.message,
      reminder.ticker,
      'dismissed'
    ).subscribe({
      next: () => {
        this.loadReminders();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  deleteReminder(reminder: Reminder) {
    if (!confirm(`Delete reminder "${reminder.message}"?`)) return;

    this.loading = true;
    this.errors = [];

    this.stocksService.deleteReminder(reminder.reminderId).subscribe({
      next: () => {
        this.loadReminders();
        this.loading = false;
      },
      error: (error) => {
        this.errors = GetErrors(error);
        this.loading = false;
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  isOverdue(reminder: Reminder): boolean {
    return reminder.state === 'pending' && new Date(reminder.date) < new Date();
  }

  getStateBadgeClass(state: string): string {
    switch(state) {
      case 'pending': return 'badge bg-warning text-dark';
      case 'sent': return 'badge bg-success';
      case 'dismissed': return 'badge bg-secondary';
      default: return 'badge bg-light text-dark';
    }
  }
}
