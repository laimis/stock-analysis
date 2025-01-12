import {Injectable} from "@angular/core";
import {Observable, of} from "rxjs";
import {tap} from "rxjs/operators";
import {BrokerageAccount} from "./stocks.service";
import { HttpClient } from "@angular/common/http";

export enum BrokerageOrderType {
    Market = 'Market',
    Limit = 'Limit',
    Stop = 'Stop',
    StopLimit = 'StopLimit'
}

export enum BrokerageOrderDuration {
    Day = 'Day',
    GTC = 'GTC',
    DayPlus = 'DayPlus',
    GtcPlus = 'GtcPlus'
}

export class BrokerageOrderCommand {
    ticker: string
    numberOfShares: number
    price: number
    type: string
    duration: string
    positionId: string
    notes: string
}

// types for orders:
// Enum for order types
export enum OptionOrderType {
    NET_DEBIT = "NET_DEBIT",
    NET_CREDIT = "NET_CREDIT",
    LIMIT = "LIMIT",
}

// Enum for order instructions
export enum OptionOrderInstruction {
    BUY_TO_OPEN = "BUY_TO_OPEN",
    SELL_TO_OPEN = "SELL_TO_OPEN",
    BUY_TO_CLOSE = "BUY_TO_CLOSE",
    SELL_TO_CLOSE = "SELL_TO_CLOSE",
}

// Interface for instrument
export interface Instrument {
    symbol: string;
    assetType: string;
}

// Interface for order leg
export interface OrderLeg {
    instruction: OptionOrderInstruction;
    quantity: number;
    instrument: Instrument;
}

// Main order interface
export interface OptionOrderCommand {
    orderType: OptionOrderType;
    session: string;
    price: number;
    duration: string
    orderStrategyType: string;
    orderLegCollection: OrderLeg[];
}

@Injectable({providedIn: 'root'})
export class BrokerageService {

    // going out to get it itself
    private brokerageAccountData: BrokerageAccount = null

    constructor(private http: HttpClient) {
    }

    buy(obj: BrokerageOrderCommand): Observable<any> {
        this.brokerageAccountData = null
        return this.http.post('/api/brokerage/buy', obj)
    }

    sell(obj: BrokerageOrderCommand): Observable<any> {
        this.brokerageAccountData = null
        return this.http.post('/api/brokerage/sell', obj)
    }

    sellShort(obj: BrokerageOrderCommand): Observable<any> {
        this.brokerageAccountData = null
        return this.http.post('/api/brokerage/sellshort', obj)
    }

    buyToCover(obj: BrokerageOrderCommand): Observable<any> {
        this.brokerageAccountData = null
        return this.http.post('/api/brokerage/buytocover', obj)
    }
    
    issueOptionOrder(obj: OptionOrderCommand): Observable<any> {
        this.brokerageAccountData = null
        return this.http.post('/api/brokerage/optionsorder', obj)
    }

    cancelOrder(orderId: string): Observable<any> {
        this.brokerageAccountData = null
        return this.http.delete('/api/brokerage/orders/' + orderId)
    }

    brokerageAccount(): Observable<BrokerageAccount> {
        if (this.brokerageAccountData) {
            return of(this.brokerageAccountData)
        }
        return this.http.get<BrokerageAccount>('/api/brokerage/account').pipe(
            tap(data => this.brokerageAccountData = data),
        )
    }

}
