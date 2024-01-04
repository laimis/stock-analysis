import {Injectable} from "@angular/core";
import {Observable, of} from "rxjs";
import {tap} from "rxjs/operators";
import {BrokerageAccount, brokerageordercommand} from "./stocks.service";
import {HttpClient} from "@angular/common/http";

export enum BrokerageOrderType {
  Market = 'Market',
  Limit = 'Limit',
  Stop = 'Stop',
  StopLimit = 'StopLimit'
}

export enum BrokerageOrderDuration {
  Day = 'Day',
  Gtc = 'Gtc',
  DayPlus = 'DayPlus',
  GtcPlus = 'GtcPlus'
}

@Injectable({providedIn: 'root'})
export class BrokerageService {

  constructor(private http: HttpClient) { }

  buy(obj:brokerageordercommand) : Observable<any> {
    this.brokerageAccountData = null
    return this.http.post('/api/brokerage/buy', obj)
  }

  sell(obj:brokerageordercommand) : Observable<any> {
    this.brokerageAccountData = null
    return this.http.post('/api/brokerage/sell', obj)
  }

  sellShort(obj:brokerageordercommand) : Observable<any> {
    this.brokerageAccountData = null
    return this.http.post('/api/brokerage/sellshort', obj)
  }

  buyToCover(obj:brokerageordercommand) : Observable<any> {
    this.brokerageAccountData = null
    return this.http.post('/api/brokerage/buytocover', obj)
  }

  brokerageCancelOrder(orderId:string) : Observable<any> {
    this.brokerageAccountData = null
    return this.http.delete('/api/brokerage/orders/' + orderId)
  }

  // TODO: remove this caching approach once we figure out how
  // orders component can get orders passed to it instead of
  // going out to get it itself
  private brokerageAccountData:BrokerageAccount = null
  brokerageAccount() : Observable<BrokerageAccount> {
    if (this.brokerageAccountData) {
      return of(this.brokerageAccountData)
    }
    return this.http.get<BrokerageAccount>('/api/brokerage/account').pipe(
      tap(data => this.brokerageAccountData = data),
    )
  }

}
