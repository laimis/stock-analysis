import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SECFiling } from 'src/app/services/sec.service';

interface FilingRow {
  isGroup: boolean;
  yearMonth?: string;
  filing?: SECFiling;
  key: string;
}

@Component({
  selector: 'app-sec-filings-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sec-filings-table.component.html'
})
export class SecFilingsTableComponent {
  @Input() filings: SECFiling[] = [];
  @Input() loading = false;
  @Input() emptyMessage = 'No SEC filings found.';
  @Input() showFilter = true;

  selectedFilter: string = 'all';

  filterCategories = [
    { value: 'all', label: 'All Filings' },
    { value: 'insider', label: 'Insider Trading' },
    { value: 'financial', label: 'Financial Reports' },
    { value: 'ownership', label: 'Ownership Disclosures' },
    { value: 'proxy', label: 'Proxy Materials' },
    { value: 'registration', label: 'Registrations' }
  ];

  getFilteredFilings(): SECFiling[] {
    if (this.selectedFilter === 'all') {
      return this.filings;
    }
    return this.filings.filter(filing => this.matchesFilter(filing.filing));
  }

  getFlatFilingsWithGroups(): FilingRow[] {
    const filteredFilings = this.getFilteredFilings();
    
    if (!filteredFilings || filteredFilings.length === 0) {
      return [];
    }

    const filingsByYearMonth = this.groupFilingsByYearMonth(filteredFilings);
    const result: FilingRow[] = [];
    
    const yearMonths = Array.from(filingsByYearMonth.keys()).sort().reverse();
    
    for (const yearMonth of yearMonths) {
      result.push({ isGroup: true, yearMonth, key: 'group-' + yearMonth });
      for (const filing of filingsByYearMonth.get(yearMonth) ?? []) {
        result.push({ isGroup: false, filing, key: filing.filingUrl });
      }
    }
    
    return result;
  }

  private groupFilingsByYearMonth(filings: SECFiling[]): Map<string, SECFiling[]> {
    const groups = new Map<string, SECFiling[]>();
    
    filings.forEach(filing => {
      const date = new Date(filing.filingDate);
      const yearMonth = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      
      if (!groups.has(yearMonth)) {
        groups.set(yearMonth, []);
      }
      groups.get(yearMonth)!.push(filing);
    });
    
    return groups;
  }

  formatYearMonth(yearMonth: string): string {
    const [year, month] = yearMonth.split('-');
    const date = new Date(parseInt(year), parseInt(month) - 1);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long' });
  }

  onFilterChange() {
    // Trigger re-rendering by component template
  }

  private matchesFilter(filingType: string): boolean {
    const normalized = filingType.trim().toUpperCase();

    switch (this.selectedFilter) {
      case 'insider':
        return ['3', '4', '5', '144'].includes(normalized);
      
      case 'financial':
        return ['10-Q', '10-K', '8-K'].includes(normalized);
      
      case 'ownership':
        return normalized.includes('13D') || 
               normalized.includes('13G') || 
               normalized.includes('13F');
      
      case 'proxy':
        return normalized.includes('14A');
      
      case 'registration':
        return ['S-1', 'S-8', 'ARS'].includes(normalized) ||
               normalized.startsWith('S-');
      
      default:
        return true;
    }
  }
}
