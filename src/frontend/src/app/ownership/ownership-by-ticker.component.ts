import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipSummary, OwnershipEvent } from '../services/ownership.service';
import { StocksService, DataPoint, DataPointContainer, ChartType } from '../services/stocks.service';
import { TimeAgoPipe } from '../services/time-ago.pipe';
import { LineChartComponent } from '../shared/line-chart/line-chart.component';

const getEntityTypeDisplay = OwnershipService.getEntityTypeDisplay;

@Component({
  selector: 'app-ownership-by-ticker',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TimeAgoPipe, LineChartComponent],
  templateUrl: './ownership-by-ticker.component.html'
})
export class OwnershipByTickerComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private stocksService = inject(StocksService);
  private route = inject(ActivatedRoute);

  private _ticker = '';
  @Input() set ticker(value: string) {
    if (value && value !== this._ticker) {
      this._ticker = value;
      this.loadData();
    }
  }
  get ticker(): string { return this._ticker; }
  @Input() embedded = false;
  ownershipSummary: OwnershipSummary[] = [];
  timeline: OwnershipEvent[] = [];
  loading = false;
  error = '';
  syncing = false;
  syncMessage = '';
  getEntityTypeDisplay = getEntityTypeDisplay;
  timelineDays = 1825;
  entityNameMap = new Map<string, string>();
  sharesOutstanding: number | null = null;
  ownershipChartContainers: DataPointContainer[] = [];

  private readonly chartColors = [
    '#2196F3', '#F44336', '#4CAF50', '#FF9800', '#9C27B0',
    '#00BCD4', '#795548', '#607D8B', '#E91E63', '#FF5722'
  ];

  ngOnInit() {
    if (!this._ticker) {
      this.route.params.subscribe(params => {
        this.ticker = params['ticker'];
      });
    }
  }

  loadData() {
    this.loadOwnershipSummary();
    this.loadTimeline(this.timelineDays);
    this.loadSharesOutstanding();
  }

  loadSharesOutstanding() {
    this.stocksService.reportFundamentals([this.ticker]).subscribe({
      next: (fundamentals) => {
        if (fundamentals.length > 0) {
          const tickerFundamentals = fundamentals[0];
          const sharesOutstandingStr = tickerFundamentals.fundamentals['sharesOutstanding'];
          if (sharesOutstandingStr) {
            this.sharesOutstanding = parseFloat(sharesOutstandingStr);
          }
        }
      },
      error: (err) => {
        console.error('Failed to load shares outstanding:', err);
      }
    });
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
        this.buildOwnershipChartData();
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
        this.buildOwnershipChartData();
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

  syncOwnership() {
    this.syncing = true;
    this.syncMessage = '';
    this.ownershipService.syncOwnershipForTicker(this.ticker).subscribe({
      next: () => {
        this.syncing = false;
        this.syncMessage = 'Sync complete. Ownership data has been refreshed from SEC EDGAR.';
        this.loadData();
      },
      error: (err) => {
        this.syncing = false;
        this.syncMessage = `Sync failed: ${err.message}`;
      }
    });
  }

  calculateOwnershipPercent(shares: number): number {
    if (!this.sharesOutstanding || this.sharesOutstanding === 0) {
      return 0;
    }
    return (shares / this.sharesOutstanding) * 100;
  }

  get totalShares(): number {
    return this.ownershipSummary.reduce((sum, s) => sum + s.currentShares, 0);
  }

  get totalPercentOfClass(): number {
    return this.ownershipSummary.reduce((sum, s) => sum + (s.percentOfClass || 0), 0);
  }

  get totalOwnershipCoverage(): number {
    if (!this.sharesOutstanding || this.sharesOutstanding === 0) {
      return 0;
    }
    return (this.totalShares / this.sharesOutstanding) * 100;
  }

  buildOwnershipChartData() {
    if (this.ownershipSummary.length === 0 || this.timeline.length === 0) {
      this.ownershipChartContainers = [];
      return;
    }

    // Take top owners by current share count (max 10 for readability)
    const topOwners = [...this.ownershipSummary]
      .sort((a, b) => b.currentShares - a.currentShares)
      .slice(0, 10);

    const today = new Date().toISOString().split('T')[0];

    this.ownershipChartContainers = topOwners
      .map((owner, idx) => {
        const entityEvents = this.timeline
          .filter(e => e.entityId === owner.entity.id)
          .sort((a, b) => a.transactionDate.localeCompare(b.transactionDate));

        const dataPoints: DataPoint[] = entityEvents.map(e => ({
          value: e.sharesAfter,
          label: e.transactionDate.split('T')[0],
          isDate: true
        }));

        // Extend the line to today with the last known share count
        if (dataPoints.length > 0) {
          const lastPoint = dataPoints[dataPoints.length - 1];
          if (lastPoint.label < today) {
            dataPoints.push({ value: lastPoint.value, label: today, isDate: true });
          }
        }

        return {
          label: owner.entity.name,
          chartType: ChartType.Line,
          data: dataPoints,
          color: this.chartColors[idx % this.chartColors.length],
          includeZero: false
        } as DataPointContainer;
      })
      .filter(c => c.data.length > 0);
  }
}
