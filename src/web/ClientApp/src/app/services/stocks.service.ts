import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export function GetErrors(err:any): string[] {
  return Object.keys(err.error.errors).map<string>(v => {
    return Object.getOwnPropertyDescriptor(err.error.errors, v).value
  })
}

@Injectable({providedIn: 'root'})
export class StocksService {

	constructor(private http: HttpClient) { }

	getStockSummary(symbol:string): Observable<StockSummary> {
		return this.http.get<StockSummary>('/api/stocks/' + symbol)
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
		return this.http.post('/api/stocks/purchase', obj)
	}

	sell(obj:object) : Observable<any> {
		return this.http.post('/api/stocks/sell', obj)
	}

	openOption(obj:object) : Observable<any> {
		return this.http.post('/api/options/sell', obj)
  }

  getSoldOption(ticker:string, type:string, strikePrice:number, expiration:string) : Observable<OptionDefinition> {
    return this.http.get<OptionDefinition>('/api/options/soldoptions/' + ticker + '/' + type + '/' + strikePrice + '/' + expiration)
  }

  closeSoldOption(ticker:string, type:string, strikePrice:number, expiration:string, closePrice:number, closeDate:string) : Observable<any> {
    return this.http.get('/api/options/soldoptions/' + ticker + '/' + type + '/' + strikePrice + '/' + expiration + '/close?closePrice=' + closePrice + '&closeDate=' + closeDate)
  }

	startAnalysis(minPrice: Number, maxPrice: Number) {
		this.http.post('/api/analysis/start?min=' + minPrice + '&max=' + maxPrice, null).subscribe(() => {
			console.log('start finished')
		})
  }

  getAccountStatus() : Observable<AccountStatus> {
    return this.http.get<AccountStatus>('/api/account/status')
  }

  getAcounts() : Observable<any> {
    return this.http.get('/api/admin/users')
  }
}

export interface Portfolio {
  owned: object[]
  ownedOptions: object[]
  closedOptions: object[]
	cashedOut: object[]
	totalEarned: Number
	totalSpent: Number
	totalCashedOutSpend : number
  totalCashedOutEarnings : number
  pendingPremium : number
  optionEarnings : number
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
  premium: number
  filled: string
  breakEven: number
  risk: number
  volume : number
}

export class OptionBreakdown {
  callVolume : number
  putVolume : number
  callSpend : number
  putSpend : number
  priceBasedOnCalls : number
  priceBasedOnPuts : number
}

export interface OptionDetail {
  stockPrice: number
  expirations: string[]
  options: OptionDefinition[]
  breakdown : OptionBreakdown
}

export interface AccountStatus {
  username: String
  loggedIn: boolean
}
