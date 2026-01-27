import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CompanySearchResult {
  ticker: string;
  cik: string;
  companyName: string;
}

export interface Filing {
  description: string;
  documentUrl: string;
  filingDate: string;
  reportDate: string;
  filing: string;
  filingUrl: string;
}

export interface CompanyFilings {
  ticker: string;
  filings: Filing[];
}

export interface PortfolioFilings {
  tickerFilings: CompanyFilings[];
}

@Injectable({providedIn: 'root'})
export class SECService {
  private http = inject(HttpClient);

  searchCompanies(query: string): Observable<CompanySearchResult[]> {
    return this.http.get<CompanySearchResult[]>(`/api/sec/search?query=${encodeURIComponent(query)}`);
  }

  getFilings(ticker: string): Observable<CompanyFilings> {
    return this.http.get<CompanyFilings>(`/api/sec/filings/${ticker}`);
  }

  getPortfolioFilings(): Observable<PortfolioFilings> {
    return this.http.get<PortfolioFilings>('/api/sec/portfolio-filings');
  }
}
