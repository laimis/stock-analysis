import {Injectable} from '@angular/core';
import {OptionDefinition, OptionPosition, OptionSpread, OwnedOption} from './stocks.service';
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
        if (!expirationMap.has(value.expirationDate)) {
            expirationMap.set(value.expirationDate, [value])
        } else {
            var temp = expirationMap.get(value.expirationDate)
            temp.push(value)
        }
    })

    return expirationMap
}

@Injectable({
    providedIn: 'root'
})
export class OptionService {
    
    constructor(private http : HttpClient) {
    }
    
    getOptionPositionsForTicker(ticker: string): Observable<OptionPosition[]> {
        return this.http.get<OptionPosition[]>(`/api/options/ownership/${ticker}`)
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
