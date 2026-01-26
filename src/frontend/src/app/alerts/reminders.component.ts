import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StocksService, Reminder } from '../services/stocks.service';
import { GetErrors } from '../services/utils';
import { StockLinkAndTradingviewLinkComponent } from "../shared/stocks/stock-link-and-tradingview-link.component";
import { StockSearchComponent } from '../stocks/stock-search/stock-search.component';

@Component({
  selector: 'app-reminders',
  templateUrl: './reminders.component.html',
  styleUrls: ['./reminders.component.css'],
  imports: [CommonModule, FormsModule, StockLinkAndTradingviewLinkComponent, StockSearchComponent]
})
export class RemindersComponent implements OnInit {
  private stocksService = inject(StocksService);

  reminders: Reminder[] = [];
  filteredReminders: Reminder[] = [];
  errors: string[] = [];
  loading = false;
  showForm = false;
  filterState = 'all'; // 'all', 'pending', 'sent'
  searchText = ''; // Search by ticker or message

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
    
    // Filter by search text (ticker or message)
    if (this.searchText && this.searchText.trim()) {
      const searchTerm = this.searchText.trim().toLowerCase();
      filtered = filtered.filter(r => 
        (r.ticker && r.ticker.toLowerCase().includes(searchTerm)) ||
        r.message.toLowerCase().includes(searchTerm)
      );
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

  onTickerSelected(ticker: string) {
    this.newReminder.ticker = ticker;
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

    if (this.newReminder.date < this.today) {
      this.errors = ['Reminder date cannot be in the past'];
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

    if (this.newReminder.date < this.today) {
      this.errors = ['Reminder date cannot be in the past'];
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

  isOverdue(reminder: Reminder): boolean {
    return reminder.state === 'pending' && new Date(reminder.date) < new Date();
  }

  getStateBadgeClass(state: string): string {
    switch(state) {
      case 'pending': return 'badge bg-warning text-dark';
      case 'sent': return 'badge bg-success';
      default: return 'badge bg-light text-dark';
    }
  }

  setDateToTomorrow() {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.newReminder.date = tomorrow.toISOString().split('T')[0];
  }

  setDateToNextWeek() {
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    this.newReminder.date = nextWeek.toISOString().split('T')[0];
  }

  setDateToNextMonth() {
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    this.newReminder.date = nextMonth.toISOString().split('T')[0];
  }
}
