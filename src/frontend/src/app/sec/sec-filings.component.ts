import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { SECService, CompanySearchResult, SECFilings } from '../services/sec.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';
import { SecFilingsTableComponent } from '../shared/sec/sec-filings-table.component';

@Component({
  selector: 'app-sec-filings',
  standalone: true,
  imports: [CommonModule, FormsModule, StockLinkAndTradingviewLinkComponent, SecFilingsTableComponent],
  templateUrl: './sec-filings.component.html'
})
export class SecFilingsComponent implements OnInit {
  private secService = inject(SECService);
  private searchTerms = new Subject<string>();

  searchQuery = '';
  searchResults: CompanySearchResult[] = [];
  selectedCompany: CompanySearchResult | null = null;
  selectedFilings: SECFilings | null = null;
  portfolioFilings: SECFilings[] = [];
  highlightedIndex = -1;
  
  loading = {
    search: false,
    filings: false,
    portfolio: false
  };
  
  errors: string[] = [];
  activeTab: 'search' | 'portfolio' = 'search';

  ngOnInit() {
    this.loadPortfolioFilings();
    this.setupAutoSearch();
  }

  setupAutoSearch() {
    this.searchTerms.pipe(
      debounceTime(300),
      switchMap((term: string) => {
        if (!term.trim()) {
          this.searchResults = [];
          this.loading.search = false;
          return [];
        }
        this.loading.search = true;
        this.errors = [];
        return this.secService.searchCompanies(term);
      })
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.loading.search = false;
        this.highlightedIndex = -1;
      },
      error: (error) => {
        this.errors = [`Failed to search companies: ${error.message}`];
        this.loading.search = false;
        this.searchResults = [];
      }
    });
  }

  onTabChange(tab: 'search' | 'portfolio') {
    this.activeTab = tab;
  }

  onSearchChange(value: string) {
    this.searchTerms.next(value);
  }

  onKeyDown(event: KeyboardEvent) {
    if (this.searchResults.length > 0) {
      if (event.key === 'ArrowUp' && this.highlightedIndex > 0) {
        event.preventDefault();
        this.highlightedIndex--;
      }

      if (event.key === 'ArrowDown' && this.highlightedIndex < this.searchResults.length - 1) {
        event.preventDefault();
        this.highlightedIndex++;
      }

      if (event.key === 'Enter' && this.highlightedIndex >= 0) {
        event.preventDefault();
        const company = this.searchResults[this.highlightedIndex];
        this.selectCompany(company);
      }
    }
  }

  onBlur() {
    // Delay to allow click events to fire
    setTimeout(() => {
      if (!this.selectedCompany) {
        this.searchResults = [];
      }
    }, 200);
  }

  selectCompany(company: CompanySearchResult) {
    this.selectedCompany = company;
    this.selectedFilings = null;
    this.searchQuery = `${company.ticker} - ${company.companyName}`;
    this.searchResults = [];
    this.loadFilings(company.ticker);
  }

  loadFilings(ticker: string) {
    this.loading.filings = true;
    this.errors = [];

    this.secService.getFilings(ticker).subscribe({
      next: (filings) => {
        this.selectedFilings = filings;
        this.loading.filings = false;
      },
      error: (error) => {
        this.errors.push(`Failed to load filings: ${error.message}`);
        this.loading.filings = false;
      }
    });
  }

  loadPortfolioFilings() {
    this.loading.portfolio = true;
    this.errors = [];

    this.secService.getPortfolioFilings().subscribe({
      next: (data) => {
        this.portfolioFilings = data.tickerFilings;
        this.loading.portfolio = false;
      },
      error: (error) => {
        this.errors.push(`Failed to load portfolio filings: ${error.message}`);
        this.loading.portfolio = false;
      }
    });
  }

  clearSelection() {
    this.selectedCompany = null;
    this.selectedFilings = null;
    this.searchQuery = '';
    this.searchResults = [];
  }
}
