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

  getOptions(ticker:string): Observable<OptionDetail> {
    return this.http.get<OptionDetail>('/api/options/' + ticker)
  }

	getPortfolio(): Observable<Portfolio> {
		return this.http.get<Portfolio>('/api/portfolio')
	}

	getAnalysis(sortby:string, sortdirection:string): Observable<object[]> {
		return this.http.get<object[]>('/api/analysis?sortby=' + sortby + '&sortdirection=' + sortdirection)
	}

	purchase(obj:object) : Observable<any> {
		return this.http.post('/api/portfolio/purchase', obj)
	}

	sell(obj:object) : Observable<any> {
		return this.http.post('/api/portfolio/sell', obj)
	}

	openOption(obj:object) : Observable<any> {
		return this.http.post('/api/portfolio/open', obj)
	}

	startAnalysis(minPrice: Number, maxPrice: Number) {
		this.http.post('/api/analysis/start?min=' + minPrice + '&max=' + maxPrice, null).subscribe(() => {
			console.log('start finished')
		})
	}
}

export interface Portfolio {
  owned: object[]
  options: object[]
	cashedOut: object[]
	totalEarned: Number
	totalSpent: Number
	totalCashedOutSpend : number
  totalCashedOutEarnings : number
  pendingPremium : number
  collateralCash : number
  collateralShares : number
}

export interface StockSummary {
	priceChartData : object[]
	volumeChartData : object[]
	peChartData : object[]
	bookChartData : object[]
}

export class OptionDefinition {
  ticker: string
  strikePrice: number
  expirationDate: string
  optionType: string
  amount: number
  bid: number
  filled: string
  breakEven: number
  risk: number
}

export interface OptionDetail {
  stockPrice: number
  expirations: string[]
  options: OptionDefinition[]
}
