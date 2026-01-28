import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Reminder } from '../services/stocks.service';
import { StockSearchComponent } from '../stocks/stock-search/stock-search.component';

@Component({
  selector: 'app-reminder-form',
  templateUrl: './reminder-form.component.html',
  styleUrls: ['./reminder-form.component.css'],
  imports: [CommonModule, FormsModule, StockSearchComponent]
})
export class ReminderFormComponent implements OnChanges {
  @Input() editingReminder: Reminder | null = null;
  @Input() presetTicker?: string;  // Pre-fill ticker (e.g., from stock-details page)
  @Input() showTickerField: boolean = true;  // Hide ticker field if preset
  @Input() loading: boolean = false;
  @Input() errors: string[] = [];
  
  @Output() save = new EventEmitter<{ date: string; message: string; ticker?: string }>();
  @Output() cancel = new EventEmitter<void>();

  reminderData = {
    date: '',
    message: '',
    ticker: ''
  };

  today = new Date().toISOString().split('T')[0];

  ngOnChanges(changes: SimpleChanges) {
    if (changes['editingReminder'] && this.editingReminder) {
      this.reminderData = {
        date: new Date(this.editingReminder.date).toISOString().split('T')[0],
        message: this.editingReminder.message,
        ticker: this.editingReminder.ticker || ''
      };
    }

    if (changes['presetTicker'] && this.presetTicker) {
      this.reminderData.ticker = this.presetTicker;
    }
  }

  onTickerSelected(ticker: string) {
    this.reminderData.ticker = ticker;
  }

  onSubmit() {
    if (!this.reminderData.date || !this.reminderData.message.trim()) {
      return;
    }

    const ticker = this.showTickerField 
      ? (this.reminderData.ticker.trim() || undefined)
      : this.presetTicker;

    this.save.emit({
      date: this.reminderData.date,
      message: this.reminderData.message,
      ticker
    });
  }

  onCancel() {
    this.reset();
    this.cancel.emit();
  }

  reset() {
    this.reminderData = {
      date: '',
      message: '',
      ticker: this.presetTicker || ''
    };
  }

  setDateToTomorrow() {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.reminderData.date = tomorrow.toISOString().split('T')[0];
  }

  setDateToNextWeek() {
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    this.reminderData.date = nextWeek.toISOString().split('T')[0];
  }

  setDateToNextMonth() {
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    this.reminderData.date = nextMonth.toISOString().split('T')[0];
  }
}
