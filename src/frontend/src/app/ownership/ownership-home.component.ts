import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { OwnershipService, OwnershipEntity } from '../services/ownership.service';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

@Component({
  selector: 'app-ownership-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './ownership-home.component.html'
})
export class OwnershipHomeComponent {
  private ownershipService = inject(OwnershipService);
  private router = inject(Router);
  private entitySearchTerms = new Subject<string>();

  tickerSearch = '';
  entitySearch = '';
  entitySearchResults: OwnershipEntity[] = [];

  constructor() {
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
    this.entitySearchTerms.next(this.entitySearch);
  }

  searchByTicker() {
    if (this.tickerSearch.trim()) {
      this.router.navigate(['/ownership/ticker', this.tickerSearch.toUpperCase()]);
    }
  }
}
