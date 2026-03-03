import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipEntity, OwnershipEntityCompanyRole, OwnershipEvent } from '../services/ownership.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';

const getEntityTypeDisplay = OwnershipService.getEntityTypeDisplay;
const getEventTypeDisplay = OwnershipService.getEventTypeDisplay;

@Component({
  selector: 'app-ownership-by-entity',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, StockLinkAndTradingviewLinkComponent],
  templateUrl: './ownership-by-entity.component.html'
})
export class OwnershipByEntityComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private route = inject(ActivatedRoute);

  entityId = '';
  entity: OwnershipEntity | null = null;
  roles: OwnershipEntityCompanyRole[] = [];
  events: OwnershipEvent[] = [];
  loading = false;
  error = '';
  getEntityTypeDisplay = getEntityTypeDisplay;
  getEventTypeDisplay = getEventTypeDisplay;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.entityId = params['entityId'];
      this.loadData();
    });
  }

  loadData() {
    this.loading = true;
    this.error = '';

    // Load entity, roles, and events in parallel
    this.ownershipService.getEntity(this.entityId).subscribe({
      next: (entity) => {
        this.entity = entity;
        this.loading = false;
      },
      error: (err) => {
        this.error = `Failed to load entity: ${err.message}`;
        this.loading = false;
      }
    });

    this.ownershipService.getEntityRoles(this.entityId).subscribe({
      next: (roles) => {
        this.roles = roles;
      },
      error: (err) => {
        console.error('Failed to load roles:', err);
      }
    });

    this.ownershipService.getEventsByEntity(this.entityId).subscribe({
      next: (events) => {
        this.events = events;
      },
      error: (err) => {
        console.error('Failed to load events:', err);
      }
    });
  }
}
