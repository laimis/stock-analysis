import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, CreateOwnershipEventRequest, OwnershipEntity } from '../services/ownership.service';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

@Component({
  selector: 'app-add-ownership-event',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './add-ownership-event.component.html'
})
export class AddOwnershipEventComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private entitySearchTerms = new Subject<string>();

  ticker = '';
  selectedEntity: OwnershipEntity | null = null;
  entitySearchQuery = '';
  entitySearchResults: OwnershipEntity[] = [];
  
  newEntity = {
    name: '',
    entityType: '',
    cik: ''
  };

  event: Partial<CreateOwnershipEventRequest> = {
    companyTicker: '',
    companyCik: '',
    eventType: '',
    transactionType: '',
    sharesAfter: 0,
    transactionDate: new Date().toISOString().split('T')[0],
    filingDate: new Date().toISOString().split('T')[0],
    isDirect: true
  };

  submitting = false;
  error = '';
  success = '';

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.ticker = params['ticker'];
      if (this.ticker) {
        this.event.companyTicker = this.ticker.toUpperCase();
      }
    });

    this.setupEntitySearch();
  }

  setupEntitySearch() {
    this.entitySearchTerms.pipe(
      debounceTime(300)
    ).subscribe(term => {
      if (!term.trim()) {
        this.entitySearchResults = [];
        return;
      }

      this.ownershipService.searchEntities(term).subscribe({
        next: (results) => {
          this.entitySearchResults = results;
        },
        error: (err) => {
          console.error('Failed to search entities:', err);
        }
      });
    });
  }

  onEntitySearchChange() {
    this.entitySearchTerms.next(this.entitySearchQuery);
  }

  selectEntity(entity: OwnershipEntity) {
    this.selectedEntity = entity;
    this.entitySearchQuery = entity.name;
    this.entitySearchResults = [];
  }

  createEntity() {
    if (!this.newEntity.name || !this.newEntity.entityType) {
      return;
    }

    this.ownershipService.createEntity({
      name: this.newEntity.name,
      entityType: this.newEntity.entityType,
      cik: this.newEntity.cik || null
    }).subscribe({
      next: (result) => {
        // After creating, fetch the entity to get the full object
        this.ownershipService.searchEntities(this.newEntity.name).subscribe({
          next: (entities) => {
            const created = entities.find(e => e.name === this.newEntity.name);
            if (created) {
              this.selectEntity(created);
              this.success = 'Entity created successfully!';
              this.newEntity = { name: '', entityType: '', cik: '' };
            }
          }
        });
      },
      error: (err) => {
        this.error = `Failed to create entity: ${err.message}`;
      }
    });
  }

  onSubmit() {
    if (!this.selectedEntity) {
      this.error = 'Please select or create an entity';
      return;
    }

    const request: CreateOwnershipEventRequest = {
      entityId: this.selectedEntity.id,
      companyTicker: this.event.companyTicker!,
      companyCik: this.event.companyCik!,
      filingId: null,
      eventType: this.event.eventType!,
      transactionType: this.event.transactionType || null,
      sharesBefore: this.event.sharesBefore || null,
      sharesTransacted: this.event.sharesTransacted || null,
      sharesAfter: this.event.sharesAfter!,
      percentOfClass: this.event.percentOfClass || null,
      pricePerShare: this.event.pricePerShare || null,
      totalValue: this.event.totalValue || null,
      transactionDate: this.event.transactionDate!,
      filingDate: this.event.filingDate!,
      isDirect: this.event.isDirect!,
      ownershipNature: this.event.ownershipNature || null
    };

    this.submitting = true;
    this.error = '';

    this.ownershipService.createOwnershipEvent(request).subscribe({
      next: () => {
        this.success = 'Ownership event saved successfully!';
        this.submitting = false;
        
        // Navigate back to ticker ownership view after a delay
        setTimeout(() => {
          this.router.navigate(['/ownership/ticker', this.event.companyTicker]);
        }, 1500);
      },
      error: (err) => {
        this.error = `Failed to save ownership event: ${err.message}`;
        this.submitting = false;
      }
    });
  }

  goBack() {
    if (this.ticker) {
      this.router.navigate(['/ownership/ticker', this.ticker]);
    } else {
      this.router.navigate(['/ownership']);
    }
  }
}
