import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CompanySearchResult {
  ticker: string;
  cik: string;
  companyName: string;
}

export interface SECFiling {
    description: string
    documentUrl: string
    filingDate: string
    reportDate: string | null
    filing: string
    filingUrl: string
}

export interface SECFilings {
    ticker: string
    filings: SECFiling[]
}

export interface PortfolioFilings {
  tickerFilings: SECFilings[];
}

@Injectable({providedIn: 'root'})
export class SECService {
  private http = inject(HttpClient);

  searchCompanies(query: string): Observable<CompanySearchResult[]> {
    return this.http.get<CompanySearchResult[]>(`/api/sec/search?query=${encodeURIComponent(query)}`);
  }

  getFilings(ticker: string): Observable<SECFilings> {
    return this.http.get<SECFilings>(`/api/sec/filings/${ticker}`);
  }

  getPortfolioFilings(): Observable<PortfolioFilings> {
    return this.http.get<PortfolioFilings>('/api/sec/portfolio-filings');
  }
}
