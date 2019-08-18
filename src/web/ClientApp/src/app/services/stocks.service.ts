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

	getJobs(): Observable<JobsResponse> {
		return this.http.get<JobsResponse>('/api/analysis/jobs')
	}

	getJob(jobId:string): Observable<JobResponse> {
		return this.http.get<JobResponse>('/api/analysis/jobs/' + jobId)
	}

	startAnalysis(amount: Number) {
		this.http.post('/api/analysis/start?maxCost=' + amount, null).subscribe(() => {
			console.log('start finished')
		})
	}
}

export interface JobsResponse {
	jobs: object[]
}

export interface JobResponse {
	analyzed : Number
	candidates : string[]
	toAnalyze : Number
}

export interface StockSummary {
	priceLabels : string[]
	priceValues : Number[]
	lowValues : Number[]
	highValues : Number[]
	volumeValues : Number[]
}
