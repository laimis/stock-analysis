import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { OwnershipService, CreateOwnershipEventRequest, OwnershipEntity } from '../services/ownership.service';
import { SECService, CompanySearchResult } from '../services/sec.service';
import { Subject } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';

const getEntityTypeDisplay = OwnershipService.getEntityTypeDisplay;

@Component({
  selector: 'app-add-ownership-event',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './add-ownership-event.component.html'
})
export class AddOwnershipEventComponent implements OnInit {
  private ownershipService = inject(OwnershipService);
  private secService = inject(SECService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private entitySearchTerms = new Subject<string>();
  private entityCikSearchTerms = new Subject<string>();
  private companySearchTerms = new Subject<string>();

  ticker = '';
  selectedEntity: OwnershipEntity | null = null;
  entitySearchQuery = '';
  entitySearchResults: OwnershipEntity[] = [];
  entityCikSearchResults: CompanySearchResult[] = [];
  companySearchResults: CompanySearchResult[] = [];
  highlightedEntityIndex = -1;
  highlightedCikIndex = -1;
  highlightedCompanyIndex = -1;
  loading = {
    entitySearch: false,
    cikSearch: false,
    companySearch: false
  };
  
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
  getEntityTypeDisplay = getEntityTypeDisplay;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.ticker = params['ticker'];
      if (this.ticker) {
        this.event.companyTicker = this.ticker.toUpperCase();
        // Auto-lookup company CIK if ticker is provided
        this.companySearchTerms.next(this.ticker);
      }
    });

    this.setupEntitySearch();
    this.setupCikSearch();
    this.setupCompanySearch();
  }

  setupEntitySearch() {
    this.entitySearchTerms.pipe(
      debounceTime(300)
    ).subscribe(term => {
      if (!term.trim()) {
        this.entitySearchResults = [];
        this.loading.entitySearch = false;
        return;
      }

      this.loading.entitySearch = true;
      this.ownershipService.searchEntities(term).subscribe({
        next: (results) => {
          this.entitySearchResults = results;
          this.loading.entitySearch = false;
          this.highlightedEntityIndex = -1;
        },
        error: (err) => {
          console.error('Failed to search entities:', err);
          this.loading.entitySearch = false;
        }
      });
    });
  }

  setupCikSearch() {
    this.entityCikSearchTerms.pipe(
      debounceTime(300),
      switchMap((term: string) => {
        if (!term.trim()) {
          this.entityCikSearchResults = [];
          this.loading.cikSearch = false;
          return [];
        }
        this.loading.cikSearch = true;
        return this.secService.searchCompanies(term);
      })
    ).subscribe({
      next: (results) => {
        this.entityCikSearchResults = results;
        this.loading.cikSearch = false;
        this.highlightedCikIndex = -1;
      },
      error: (err) => {
        console.error('Failed to search CIK:', err);
        this.loading.cikSearch = false;
        this.entityCikSearchResults = [];
      }
    });
  }

  setupCompanySearch() {
    this.companySearchTerms.pipe(
      debounceTime(300),
      switchMap((term: string) => {
        if (!term.trim()) {
          this.companySearchResults = [];
          this.loading.companySearch = false;
          return [];
        }
        this.loading.companySearch = true;
        return this.secService.searchCompanies(term);
      })
    ).subscribe({
      next: (results) => {
        this.companySearchResults = results;
        this.loading.companySearch = false;
        this.highlightedCompanyIndex = -1;
        // Auto-fill if exact match and only one result
        if (results.length === 1 && !this.event.companyCik) {
          this.selectCompany(results[0]);
        }
      },
      error: (err) => {
        console.error('Failed to search company:', err);
        this.loading.companySearch = false;
        this.companySearchResults = [];
      }
    });
  }

  onEntitySearchChange() {
    this.entitySearchTerms.next(this.entitySearchQuery);
  }

  onEntityCikSearchChange() {
    this.entityCikSearchTerms.next(this.newEntity.name);
  }

  onCompanyTickerChange() {
    this.companySearchTerms.next(this.event.companyTicker || '');
  }

  selectEntity(entity: OwnershipEntity) {
    this.selectedEntity = entity;
    this.entitySearchQuery = entity.name;
    this.entitySearchResults = [];
    this.highlightedEntityIndex = -1;
  }

  selectEntityCik(company: CompanySearchResult) {
    this.newEntity.name = company.companyName;
    this.newEntity.cik = company.cik;
    this.entityCikSearchResults = [];
    this.highlightedCikIndex = -1;
  }

  selectCompany(company: CompanySearchResult) {
    this.event.companyTicker = company.ticker;
    this.event.companyCik = company.cik;
    this.companySearchResults = [];
    this.highlightedCompanyIndex = -1;
  }

  // Keyboard navigation for entity search
  onEntityKeyDown(event: KeyboardEvent) {
    if (this.entitySearchResults.length > 0) {
      if (event.key === 'ArrowUp' && this.highlightedEntityIndex > 0) {
        event.preventDefault();
        this.highlightedEntityIndex--;
      }
      if (event.key === 'ArrowDown' && this.highlightedEntityIndex < this.entitySearchResults.length - 1) {
        event.preventDefault();
        this.highlightedEntityIndex++;
      }
      if (event.key === 'Enter' && this.highlightedEntityIndex >= 0) {
        event.preventDefault();
        this.selectEntity(this.entitySearchResults[this.highlightedEntityIndex]);
      }
      if (event.key === 'Escape') {
        this.entitySearchResults = [];
        this.highlightedEntityIndex = -1;
      }
    }
  }

  // Keyboard navigation for CIK search
  onCikKeyDown(event: KeyboardEvent) {
    if (this.entityCikSearchResults.length > 0) {
      if (event.key === 'ArrowUp' && this.highlightedCikIndex > 0) {
        event.preventDefault();
        this.highlightedCikIndex--;
      }
      if (event.key === 'ArrowDown' && this.highlightedCikIndex < this.entityCikSearchResults.length - 1) {
        event.preventDefault();
        this.highlightedCikIndex++;
      }
      if (event.key === 'Enter' && this.highlightedCikIndex >= 0) {
        event.preventDefault();
        this.selectEntityCik(this.entityCikSearchResults[this.highlightedCikIndex]);
      }
      if (event.key === 'Escape') {
        this.entityCikSearchResults = [];
        this.highlightedCikIndex = -1;
      }
    }
  }

  // Keyboard navigation for company search
  onCompanyKeyDown(event: KeyboardEvent) {
    if (this.companySearchResults.length > 0) {
      if (event.key === 'ArrowUp' && this.highlightedCompanyIndex > 0) {
        event.preventDefault();
        this.highlightedCompanyIndex--;
      }
      if (event.key === 'ArrowDown' && this.highlightedCompanyIndex < this.companySearchResults.length - 1) {
        event.preventDefault();
        this.highlightedCompanyIndex++;
      }
      if (event.key === 'Enter' && this.highlightedCompanyIndex >= 0) {
        event.preventDefault();
        this.selectCompany(this.companySearchResults[this.highlightedCompanyIndex]);
      }
      if (event.key === 'Escape') {
        this.companySearchResults = [];
        this.highlightedCompanyIndex = -1;
      }
    }
  }

  // Blur handlers to close dropdowns
  onEntityBlur() {
    setTimeout(() => {
      this.entitySearchResults = [];
      this.highlightedEntityIndex = -1;
    }, 200);
  }

  onCikBlur() {
    setTimeout(() => {
      this.entityCikSearchResults = [];
      this.highlightedCikIndex = -1;
    }, 200);
  }

  onCompanyBlur() {
    setTimeout(() => {
      this.companySearchResults = [];
      this.highlightedCompanyIndex = -1;
    }, 200);
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
