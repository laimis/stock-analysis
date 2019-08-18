import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class StocksService {

	constructor(private http: HttpClient) { }

	getStockSummary(symbol:string): Observable<StockSummary> {
		return this.http.get<StockSummary>('/api/stocks/' + symbol)
	}

	getStocks(): Observable<object> {
		return this.http.get<object>('/api/stocks')
	}

	getDashboard(): Observable<object> {
		return this.http.get<Object>('/api/dashboard')
	}
}

export interface StockSummary {
	priceLabels : string[]
	priceValues : Number[]
	lowValues : Number[]
	highValues : Number[]
	volumeValues : Number[]
}
