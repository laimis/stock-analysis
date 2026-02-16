import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipSummary, OwnershipEvent } from '../services/ownership.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';

@Component({
  selector: 'app-ownership-by-ticker',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, StockLinkAndTradingviewLinkComponent],
  templateUrl: './ownership-by-ticker.component.html'
})
export class OwnershipByTickerComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private route = inject(ActivatedRoute);

  ticker = '';
  ownershipSummary: OwnershipSummary[] = [];
  timeline: OwnershipEvent[] = [];
  loading = false;
  error = '';
  timelineDays = 365;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.ticker = params['ticker'];
      this.loadData();
    });
  }

  loadData() {
    this.loadOwnershipSummary();
    this.loadTimeline(this.timelineDays);
  }

  loadOwnershipSummary() {
    this.loading = true;
    this.error = '';
    
    this.ownershipService.getOwnershipByTicker(this.ticker).subscribe({
      next: (summary) => {
        this.ownershipSummary = summary;
        this.loading = false;
      },
      error: (err) => {
        this.error = `Failed to load ownership summary: ${err.message}`;
        this.loading =false;
      }
    });
  }

  loadTimeline(days: number) {
    this.timelineDays = days;
    
    this.ownershipService.getOwnershipTimeline(this.ticker, days).subscribe({
      next: (timeline) => {
        this.timeline = timeline;
      },
      error: (err) => {
        this.error = `Failed to load ownership timeline: ${err.message}`;
      }
    });
  }
}
