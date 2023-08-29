import { Injectable } from '@angular/core';
import { OptionDefinition, OptionSpread } from './stocks.service';


@Injectable({
  providedIn: 'root'
})
export class OptionService {
  findBullPutSpreads(options: OptionDefinition[]): OptionSpread[] {
    let expirationMap = new Map<string, OptionDefinition[]>();
    options.forEach(function(value, index, arr) {
      if (!expirationMap.has(value.expirationDate))
      {
        expirationMap.set(value.expirationDate, [value])
      }
      else
      {
          var temp = expirationMap.get(value.expirationDate)
          temp.push(value)
      }
    })

    // find spreads
    let spreads = new Array<OptionSpread>()
    expirationMap.forEach(function(value, key, map) {
      let puts = value.filter(x => x.optionType == "put")


      puts.forEach(function(buyOption, index, arr) {
        // we will buy the call at the index, and sell the next call, if available.
        if (index + 1 < puts.length) {
          let sellOption = puts[index + 1]

          let maxGain = sellOption.bid - buyOption.ask
          let maxCost = maxGain
          let maxLoss = sellOption.strikePrice - buyOption.strikePrice - maxGain

          let legs : OptionSpread = {
            name: "Bull Put Spread " + sellOption.strikePrice + "-" + buyOption.strikePrice,
            legs: [buyOption, sellOption],
            maxCost: maxCost,
            maxGain: maxGain,
            maxLoss: maxLoss
          }
          
          spreads.push(legs)
        }
      })
    })

    return spreads
  }

  findStraddles(options: OptionDefinition[]): OptionSpread[] {
    // group options by expiration
    let expirationMap = new Map<string, OptionDefinition[]>();
    options.forEach(function(value, index, arr) {
      if (!expirationMap.has(value.expirationDate))
      {
        expirationMap.set(value.expirationDate, [value])
      }
      else
      {
          var temp = expirationMap.get(value.expirationDate)
          temp.push(value)
      }
    })

    // find straddles
    let straddles = new Array<OptionSpread>()
    expirationMap.forEach(function(value, key, map) {
      let calls = value.filter(x => x.optionType == "call")
      let puts = value.filter(x => x.optionType == "put")

      calls.forEach(function(call, index, arr) {
        puts.forEach(function(put, index, arr) {
          if (call.strikePrice == put.strikePrice) {
            // overall bid
            let bid = call.bid + put.bid
            // overall ask
            let ask = call.ask + put.ask
            
            let legs : OptionSpread = {
              name: "Straddle " + call.strikePrice,
              legs: [call, put],
              maxCost: ask,
              maxLoss: Infinity,
              maxGain: Infinity
            }
            
            straddles.push(legs)
          }
        })
      })
    })

    return straddles
  }

  findBullCallSpreads(options:OptionDefinition[]) {
    let expirationMap = new Map<string, OptionDefinition[]>();
    options.forEach(function(value, index, arr) {
      if (!expirationMap.has(value.expirationDate))
      {
        expirationMap.set(value.expirationDate, [value])
      }
      else
      {
          var temp = expirationMap.get(value.expirationDate)
          temp.push(value)
      }
    })

    // find spreads
    let spreads = new Array<OptionSpread>()
    expirationMap.forEach(function(value, key, map) {
      let calls = value.filter(x => x.optionType == "call")


      calls.forEach(function(buyOption, index, arr) {
        // we will buy the call at the index, and sell the next call, if available.
        if (index + 1 < calls.length) {
          let sellOption = calls[index + 1]

          let maxCost = buyOption.ask - sellOption.bid
          let maxGain = (sellOption.strikePrice - buyOption.strikePrice) - (buyOption.ask - sellOption.bid)

          let legs : OptionSpread = {
            name: "Bull Call Spread " + buyOption.strikePrice + "-" + sellOption.strikePrice,
            legs: [buyOption, sellOption],
            maxCost: maxCost,
            maxGain: maxGain,
            maxLoss: (buyOption.ask - sellOption.bid)
          }
          
          spreads.push(legs)
        }
      })
    })

    return spreads
  }
}
