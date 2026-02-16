import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipSummary, OwnershipEvent } from '../services/ownership.service';

const getEntityTypeDisplay = OwnershipService.getEntityTypeDisplay;

@Component({
  selector: 'app-ownership-by-ticker',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
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
  getEntityTypeDisplay = getEntityTypeDisplay;
  timelineDays = 365;
  entityNameMap = new Map<string, string>();

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
        // Build entity name lookup map
        this.entityNameMap.clear();
        summary.forEach(s => {
          this.entityNameMap.set(s.entity.id, s.entity.name);
        });
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
        // Load entity names for any entities not in summary
        const missingEntityIds = timeline
          .map(e => e.entityId)
          .filter(id => !this.entityNameMap.has(id));
        
        if (missingEntityIds.length > 0) {
          // Load entity details for missing entities
          missingEntityIds.forEach(id => {
            this.ownershipService.getEntity(id).subscribe({
              next: (entity) => this.entityNameMap.set(entity.id, entity.name),
              error: () => {} // Silently fail, will show ID
            });
          });
        }
      },
      error: (err) => {
        this.error = `Failed to load ownership timeline: ${err.message}`;
      }
    });
  }

  getEntityName(entityId: string): string {
    return this.entityNameMap.get(entityId) || entityId;
  }

  get totalShares(): number {
    return this.ownershipSummary.reduce((sum, s) => sum + s.currentShares, 0);
  }

  get totalPercentOfClass(): number {
    return this.ownershipSummary.reduce((sum, s) => sum + (s.percentOfClass || 0), 0);
  }
}
