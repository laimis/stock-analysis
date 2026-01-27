import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SECService, CompanySearchResult, CompanyFilings, Filing } from '../services/sec.service';
import { StockLinkAndTradingviewLinkComponent } from '../shared/stocks/stock-link-and-tradingview-link.component';

@Component({
  selector: 'app-sec-filings',
  standalone: true,
  imports: [CommonModule, FormsModule, StockLinkAndTradingviewLinkComponent],
  templateUrl: './sec-filings.component.html'
})
export class SecFilingsComponent implements OnInit {
  private secService = inject(SECService);

  searchQuery = '';
  searchResults: CompanySearchResult[] = [];
  selectedCompany: CompanySearchResult | null = null;
  selectedFilings: CompanyFilings | null = null;
  portfolioFilings: CompanyFilings[] = [];
  
  loading = {
    search: false,
    filings: false,
    portfolio: false
  };
  
  errors: string[] = [];
  activeTab: 'search' | 'portfolio' = 'search';

  ngOnInit() {
    this.loadPortfolioFilings();
  }

  onTabChange(tab: 'search' | 'portfolio') {
    this.activeTab = tab;
  }

  searchCompanies() {
    if (!this.searchQuery.trim()) {
      this.searchResults = [];
      return;
    }

    this.loading.search = true;
    this.errors = [];

    this.secService.searchCompanies(this.searchQuery).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.loading.search = false;
      },
      error: (error) => {
        this.errors.push(`Failed to search companies: ${error.message}`);
        this.loading.search = false;
      }
    });
  }

  selectCompany(company: CompanySearchResult) {
    this.selectedCompany = company;
    this.selectedFilings = null;
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

  formatDate(dateString: string): Date {
    return new Date(dateString);
  }

  groupFilingsByMonth(filings: Filing[]): { yearMonth: string; filings: Filing[] }[] {
    const groups = new Map<string, Filing[]>();
    
    filings.forEach(filing => {
      const date = new Date(filing.filingDate);
      const yearMonth = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      
      if (!groups.has(yearMonth)) {
        groups.set(yearMonth, []);
      }
      groups.get(yearMonth)!.push(filing);
    });
    
    return Array.from(groups.entries())
      .map(([yearMonth, filings]) => ({ yearMonth, filings }))
      .sort((a, b) => b.yearMonth.localeCompare(a.yearMonth));
  }

  formatYearMonth(yearMonth: string): string {
    const [year, month] = yearMonth.split('-');
    const date = new Date(parseInt(year), parseInt(month) - 1);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long' });
  }
}
