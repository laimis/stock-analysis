import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SECFiling {
  filing: string;
  filingDate: string;
  reportDate: string | null;
  description: string | null;
  filingUrl: string;
  documentUrl: string;
}

interface FilingRow {
  isGroup: boolean;
  yearMonth?: string;
  filing?: SECFiling;
  key: string;
}

@Component({
  selector: 'app-sec-filings-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sec-filings-table.component.html'
})
export class SecFilingsTableComponent {
  @Input() filings: SECFiling[] = [];
  @Input() loading = false;
  @Input() emptyMessage = 'No SEC filings found.';

  getFlatFilingsWithGroups(): FilingRow[] {
    if (!this.filings || this.filings.length === 0) {
      return [];
    }

    const filingsByYearMonth = this.groupFilingsByYearMonth(this.filings);
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
}
