import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipEntity, OwnershipEvent } from '../services/ownership.service';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';
import { TimeAgoPipe } from '../services/time-ago.pipe';

const getEntityTypeDisplay = OwnershipService.getEntityTypeDisplay;

@Component({
  selector: 'app-ownership-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, StockLinkAndTradingviewLinkComponent, TimeAgoPipe],
  templateUrl: './ownership-home.component.html'
})
export class OwnershipHomeComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private router = inject(Router);
  private entitySearchTerms = new Subject<string>();
  private tickerSearchTerms = new Subject<string>();

  tickerSearch = '';
  entitySearch = '';
  entitySearchResults: OwnershipEntity[] = [];
  tickerSearchResults: string[] = [];
  recentTimelines: OwnershipEvent[] = [];
  entityNameMap = new Map<string, string>();
  entityNamesLoading = false;
  getEntityTypeDisplay = getEntityTypeDisplay;
  
  loading = {
    ticker: false,
    entity: false,
    timelines: false
  };

  ngOnInit() {
    this.loadRecentTimelines();
  }

  constructor() {
    this.setupEntitySearch();
    this.setupTickerSearch();
  }

  setupTickerSearch() {
    this.tickerSearchTerms.pipe(
      debounceTime(300)
    ).subscribe(term => {
      if (!term.trim()) {
        this.tickerSearchResults = [];
        this.loading.ticker = false;
        return;
      }

      // Simple approach: show the entered ticker as a result
      this.tickerSearchResults = [term.trim().toUpperCase()];
      this.loading.ticker = false;
    });
  }

  setupEntitySearch() {
    this.entitySearchTerms.pipe(
      debounceTime(300)
    ).subscribe(term => {
      if (!term.trim()) {
        this.entitySearchResults = [];
        this.loading.entity = false;
        return;
      }

      this.loading.entity = true;
      this.ownershipService.searchEntities(term).subscribe({
        next: (results) => {
          this.entitySearchResults = results;
          this.loading.entity = false;
        },
        error: (err) => {
          console.error('Failed to search entities:', err);
          this.loading.entity = false;
        }
      });
    });
  }

  onTickerSearchChange() {
    if (this.tickerSearch.trim()) {
      this.loading.ticker = true;
    }
    this.tickerSearchTerms.next(this.tickerSearch);
  }

  onEntitySearchChange() {
    this.entitySearchTerms.next(this.entitySearch);
  }

  selectTicker(ticker: string) {
    this.router.navigate(['/ownership/ticker', ticker]);
    this.tickerSearchResults = [];
  }

  searchByTicker() {
    if (this.tickerSearch.trim()) {
      this.router.navigate(['/ownership/ticker', this.tickerSearch.toUpperCase()]);
    }
  }

  loadRecentTimelines() {
    this.loading.timelines = true;

    this.ownershipService.getRecentTimelines(25).subscribe({
      next: (timelines) => {
        this.recentTimelines = timelines;
        
        // Load all entity names in a single batch request
        const entityIds = [...new Set(timelines.map(t => t.entityId))];
        if (entityIds.length > 0) {
          this.entityNamesLoading = true;
          this.ownershipService.getEntitiesByIds(entityIds).subscribe({
            next: (entities) => {
              entities.forEach(e => this.entityNameMap.set(e.id, e.name));
              this.entityNamesLoading = false;
            },
            error: () => {
              this.entityNamesLoading = false;
            }
          });
        }
        
        this.loading.timelines = false;
      },
      error: (err) => {
        console.error('Failed to load recent timelines:', err);
        this.loading.timelines = false;
      }
    });
  }

  getEntityName(entityId: string): string {
    return this.entityNameMap.get(entityId) ?? null;
  }
}
