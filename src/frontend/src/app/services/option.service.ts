import {Injectable} from '@angular/core';
import {KeyValuePair, Note, Transaction} from './stocks.service';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";

function toBuyLeg(option: OptionDefinition) {
    return {option: option, action: 'BUY'}
}

function toSellLeg(option: OptionDefinition) {
    return {option: option, action: 'SELL'}
}

function createExpirationMap(options: OptionDefinition[]): Map<string, OptionDefinition[]> {
    let expirationMap = new Map<string, OptionDefinition[]>();
    options.forEach(function (value, index, arr) {
        if (!expirationMap.has(value.expiration)) {
            expirationMap.set(value.expiration, [value])
        } else {
            const temp = expirationMap.get(value.expiration);
            temp.push(value)
        }
    })

    return expirationMap
}

export interface OptionContract {
    underlyingTicker: string
    optionType: string
    strikePrice: number
    expiration: string
    quantity: number
    cost: number
    market: number
    pctInTheMoney: number | undefined
    details: OptionDefinition | undefined
    instruction: string | undefined
}
export interface OptionPosition {
    positionId: string
    underlyingTicker: string
    contracts: OptionContract[]
    transactions: OptionPositionTransaction[]
    cost: number
    market: number
    spread: number
    profit: number
    daysHeld: number
    daysToExpiration: number[]
    isOpen: boolean
    isClosed: boolean
    opened: string
    closed: string
    notes: Note[]
    labels: KeyValuePair[]
}
export class OptionPositionTransaction {
    expiration: string
    strike: number
    optionType: string
    quantity: number
    debited: number
    credited: number
    when: string
}

export class OptionDefinition {
    description: string
    symbol: string
    ticker: string
    openInterest: number
    strikePrice: number
    expiration: string
    optionType: string
    bid: number
    ask: number
    last: number
    mark: number
    spread: number
    perDayPrice: number
    breakEven: number
    risk: number
    volume: number
    daysToExpiration: number
    volatility: number
    delta: number
    gamma: number
    theta: number
    vega: number
    rho: number
    timeValue: number
    intrinsicValue: number
    underlyingPrice: number
}

export class OptionSpread {
    name: string
    premiumReceived?: number
    premiumPaid?: number
    risk: number
    maxGain: number
    legs: OptionLeg[]
}

export class OptionLeg {
    action: string
    option: OptionDefinition
}

export class OptionBreakdown {
    callVolume: number
    putVolume: number
    callSpend: number
    putSpend: number
    priceBasedOnCalls: number
    priceBasedOnPuts: number
}

export interface OptionChain {
    stockPrice: number
    volatility: number
    numberOfContracts: number
    expirations: string[]
    breakdown: OptionBreakdown
    options: OptionDefinition[]
}


export class OwnedOption {
    id: string
    currentPrice: number
    ticker: string
    optionType: string
    expirationDate: string
    strikePrice: number
    numberOfContracts: number
    boughtOrSold: string
    premiumReceived: number
    profit: number
    transactions: Transaction[]
    isFavorable: boolean
    itmOtmLabel: string
    strikePriceDiff: number
    days: number
    daysHeld: number
    isExpired: boolean
    expiresSoon: boolean
    assigned: boolean
    closed: string
    premiumPaid: number
    premiumCapture: number
    detail: OptionDefinition
}

export class OptionStats {
    count: number
    wins: number
    assigned: number
    averagePremiumCapture: number

    avgWinAmount: number
    losses: number
    averageLossAmount: number
    maxWinAmount: number
    maxLossAmount: number

    ev: number
    averageProfitPerDay: number
    averageDays: number
    averageDaysHeld: number
    averageDaysHeldPercentage: number
}

export interface BrokerageOptionContract {
    ticker: string
    averageCost: number
    quantity: number
    description: string
    optionType: string
    strikePrice: number
    marketValue: number
    expirationDate: string
}

export interface BrokerageOptionPosition {
    brokerageContracts: BrokerageOptionContract[]
    cost: number
    marketValue: number
    showPL: boolean
}

export interface OptionOrderLeg {
    legId : string
    cusip : string
    ticker : string
    description: string
    optionType: string
    underlyingTicker : string
    instruction: string
    quantity: number
    price: number
    expiration: string
    strikePrice: number
}

export interface BrokerageOptionOrder {
    orderId: string
    price: number
    type: string
    quantity: number
    status: string
    executionTime: string
    enteredTime: string
    canBeCancelled: boolean
    canBeRecorded: boolean
    isActive: boolean
    contracts: OptionContract[]
}

export interface OptionsContainer {
    open: OptionPosition[]
    closed: OptionPosition[]
    brokeragePositions: BrokerageOptionContract[]
    orders: BrokerageOptionOrder[]
    overallStats: OptionStats
    buyStats: OptionStats
    sellStats: OptionStats
}

export class openoptionpositioncommand {
}

export interface OptionPricing {
        optionPositionId: string
        underlyingTicker: string
        Symbol: string
        expiration: string
        strikePrice: number
        optionType: string
        volume: number
        openInterest: number
        bid: number
        ask: number
        last: number
        mark: number
        volatility: number
        delta: number
        gamma: number
        theta: number
        vega: number
        rho: number
        underlyingPrice: number
        timestamp: string
    }

@Injectable({
    providedIn: 'root'
})
export class OptionService {
    
    constructor(private http : HttpClient) {
    }
    
    getOptionPricing(symbol: string): Observable<OptionPricing[]> {
        // add symbols as part of query string, separated by commas
        // but each symbol should be URL encoded
        let url = '/api/options/pricing?symbol=' + encodeURIComponent(symbol)
        
        return this.http.get<OptionPricing[]>(url)
    }

    getOptionChain(ticker: string): Observable<OptionChain> {
        return this.http.get<OptionChain>(`/api/options/chain/${ticker}`)
    }

    getDashboard(): Observable<OptionsContainer> {
        return this.http.get<OptionsContainer>('/api/portfolio/options')
    }
    
    open(command: openoptionpositioncommand): Observable<OptionPosition> {
        return this.http.post<OptionPosition>('/api/portfolio/optionpositions', command)
    }

    get(positionId: string): Observable<OptionPosition> {
        return this.http.get<OptionPosition>('/api/portfolio/optionpositions/' + positionId)
    }

    getOptionPositionsForTicker(ticker: string): Observable<OptionPosition[]> {
        return this.http.get<OptionPosition[]>(`/api/portfolio/optionpositions/ownership/${ticker}`)
    }

    delete(id: string) {
        return this.http.delete('/api/portfolio/optionpositions/' + id)
    }
    
    deleteLabel(id: string, key: string) {
        return this.http.delete('/api/portfolio/optionpositions/' + id + '/labels/' + key)
    }
    
    setLabel(id: string, key: string, value: string) {
        return this.http.post('/api/portfolio/optionpositions/' + id + '/labels/', {key: key, value: value, positionId: id})
    }
    
    addNotes(id: string, notes: string) {
        return this.http.post('/api/portfolio/optionpositions/' + id + '/notes', notes)
    }
    
    closeContracts(id: string, contracts: any[]) {
        return this.http.post('/api/portfolio/optionpositions/' + id + '/closecontracts', contracts)
    }

    openContracts(id: string, contracts: any[]) {
        return this.http.post('/api/portfolio/optionpositions/' + id + '/opencontracts', contracts)
    }

    buyOption(obj: object): Observable<any> {
        return this.http.post<string>('/api/options/buy', obj)
    }

    sellOption(obj: object): Observable<any> {
        return this.http.post<string>('/api/options/sell', obj)
    }

    closeOption(obj: object): Observable<any> {
        return this.http.post('/api/options/close', obj)
    }

    importOptions(formData: FormData) {
        return this.http.post('/api/options/import', formData)
    }

    expireOption(optionId: string): Observable<any> {
        return this.http.post('/api/options/' + optionId + '/expire', {})
    }

    assignOption(optionId: string): Observable<any> {
        return this.http.post('/api/options/' + optionId + '/assign', {})
    }

    findBullPutSpreads(options: OptionDefinition[]): OptionSpread[] {
        let expirationMap = createExpirationMap(options)

        // find spreads
        let spreads = new Array<OptionSpread>()
        expirationMap.forEach(function (value, key, map) {
            let puts = value.filter(x => x.optionType == "put")

            puts.forEach(function (buyOption, index, arr) {
                // we will buy the lower put, and sell the put at the index, if available.
                if (index + 1 < puts.length) {
                    let sellOption = puts[index + 1]

                    let premiumReceived = sellOption.bid - buyOption.ask
                    let risk = sellOption.strikePrice - buyOption.strikePrice - premiumReceived

                    let legs: OptionSpread = {
                        name: "Bull Put Spread " + sellOption.strikePrice + "-" + buyOption.strikePrice,
                        legs: [toBuyLeg(buyOption), toSellLeg(sellOption)],
                        premiumReceived,
                        risk,
                        maxGain: premiumReceived
                    }

                    spreads.push(legs)
                }
            })
        })

        return spreads
    }

    findBearPutSpreads(options: OptionDefinition[]): OptionSpread[] {
        let expirationMap = createExpirationMap(options)

        // find spreads
        let spreads = new Array<OptionSpread>()
        expirationMap.forEach(function (value, key, map) {
            let puts = value.filter(x => x.optionType == "put")

            puts.forEach(function (sellOption, index, arr) {
                // we will sell the put at the index, and buy the next put, if available.
                if (index + 1 < puts.length) {
                    let buyOption = puts[index + 1]

                    let premiumPaid = buyOption.ask - sellOption.bid
                    let risk = premiumPaid
                    let maxGain = buyOption.strikePrice - sellOption.strikePrice - premiumPaid

                    let legs: OptionSpread = {
                        name: "Bear Put Spread " + sellOption.strikePrice + "-" + buyOption.strikePrice,
                        legs: [toSellLeg(sellOption), toBuyLeg(buyOption)],
                        premiumPaid,
                        risk,
                        maxGain
                    }

                    spreads.push(legs)
                }
            })
        })

        return spreads
    }

    findStraddles(options: OptionDefinition[]): OptionSpread[] {
        let expirationMap = createExpirationMap(options)

        // find straddles
        let straddles = new Array<OptionSpread>()
        expirationMap.forEach(function (value, key, map) {
            let calls = value.filter(x => x.optionType == "call")
            let puts = value.filter(x => x.optionType == "put")

            calls.forEach(function (call, index, arr) {
                puts.forEach(function (put, index, arr) {
                    if (call.strikePrice == put.strikePrice) {

                        let premiumPaid = call.ask + put.ask

                        let legs: OptionSpread = {
                            name: "Straddle " + call.strikePrice,
                            legs: [toBuyLeg(call), toBuyLeg(put)],
                            premiumPaid,
                            risk: premiumPaid,
                            maxGain: Infinity
                        }

                        straddles.push(legs)
                    }
                })
            })
        })

        return straddles
    }

    findBullCallSpreads(options: OptionDefinition[]) {
        let expirationMap = createExpirationMap(options)

        // find spreads
        let spreads = new Array<OptionSpread>()
        expirationMap.forEach(function (value, key, map) {
            let calls = value.filter(x => x.optionType == "call")

            calls.forEach(function (buyOption, index, arr) {
                // we will buy the call at the index, and sell the next call, if available.
                if (index + 1 < calls.length) {
                    let sellOption = calls[index + 1]

                    let premiumPaid = buyOption.ask - sellOption.bid
                    let risk = premiumPaid
                    let maxGain = (sellOption.strikePrice - buyOption.strikePrice) - premiumPaid

                    let legs: OptionSpread = {
                        name: "Bull Call Spread " + buyOption.strikePrice + "-" + sellOption.strikePrice,
                        legs: [toBuyLeg(buyOption), toSellLeg(sellOption)],
                        premiumPaid,
                        risk,
                        maxGain
                    }

                    spreads.push(legs)
                }
            })
        })

        return spreads
    }

    findBearCallSpreads(options: OptionDefinition[]) {
        let expirationMap = createExpirationMap(options)

        // find spreads
        let spreads = new Array<OptionSpread>()
        expirationMap.forEach(function (value, key, map) {
            let calls = value.filter(x => x.optionType == "call")

            calls.forEach(function (sellOption, index, arr) {
                // we will sell the call at the index, and buy the next call, if available.
                if (index + 1 < calls.length) {
                    let buyOption = calls[index + 1]

                    let premiumReceived = sellOption.bid - buyOption.ask
                    let risk = buyOption.strikePrice - sellOption.strikePrice - premiumReceived
                    let maxGain = premiumReceived

                    let legs: OptionSpread = {
                        name: "Bear Call Spread " + sellOption.strikePrice + "-" + buyOption.strikePrice,
                        legs: [toSellLeg(sellOption), toBuyLeg(buyOption)],
                        premiumReceived,
                        risk,
                        maxGain
                    }

                    spreads.push(legs)
                }
            })
        })

        return spreads
    }
}
