import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Reminder } from '../services/stocks.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';

@Component({
  selector: 'app-reminder-list',
  templateUrl: './reminder-list.component.html',
  styleUrls: ['./reminder-list.component.css'],
  imports: [CommonModule, DatePipe, StockLinkAndTradingviewLinkComponent]
})
export class ReminderListComponent {
  @Input() reminders: Reminder[] = [];
  @Input() loading: boolean = false;
  @Input() showTickerColumn: boolean = true;  // Show ticker column (hide on stock-details page)
  @Input() emptyMessage: string = 'No reminders found. Create your first reminder to get started!';
  
  @Output() edit = new EventEmitter<Reminder>();
  @Output() delete = new EventEmitter<Reminder>();

  onEdit(reminder: Reminder) {
    this.edit.emit(reminder);
  }

  onDelete(reminder: Reminder) {
    if (confirm(`Delete reminder "${reminder.message}"?`)) {
      this.delete.emit(reminder);
    }
  }

  isOverdue(reminder: Reminder): boolean {
    return reminder.state === 'pending' && new Date(reminder.date) < new Date();
  }
}
