import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StocksService, Reminder } from '../services/stocks.service';
import { GetErrors } from '../services/utils';
import { ReminderFormComponent } from './reminder-form.component';
import { ReminderListComponent } from './reminder-list.component';

@Component({
  selector: 'app-reminders',
  templateUrl: './reminders.component.html',
  styleUrls: ['./reminders.component.css'],
  imports: [CommonModule, FormsModule, ReminderFormComponent, ReminderListComponent]
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
  editingReminder: Reminder | null = null;

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

  resetForm() {
    this.editingReminder = null;
    this.errors = [];
  }

  onReminderSave(data: { date: string; message: string; ticker?: string }) {
    this.loading = true;
    this.errors = [];

    if (this.editingReminder) {
      // Update existing reminder
      this.stocksService.updateReminder(
        this.editingReminder.reminderId,
        data.date,
        data.message,
        data.ticker,
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
    } else {
      // Create new reminder
      this.stocksService.createReminder(
        data.date,
        data.message,
        data.ticker
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
  }

  editReminder(reminder: Reminder) {
    this.editingReminder = reminder;
    this.showForm = true;
  }

  deleteReminder(reminder: Reminder) {
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
}
