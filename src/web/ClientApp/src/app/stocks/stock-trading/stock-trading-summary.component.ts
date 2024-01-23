import { Component, Input } from '@angular/core';
import {BrokerageAccount, PositionInstance, StockQuote} from 'src/app/services/stocks.service';
import { isLongTermStrategy } from 'src/app/services/utils';

interface PositionGroup {
  strategy: string;
  positions: PositionInstance[];
  cost: number;
  risk: number;
  profit: number;
  length: number;
}

@Component({
  selector: 'app-stock-trading-summary',
  templateUrl: './stock-trading-summary.component.html',
  styleUrls: ['./stock-trading-summary.component.css']
})
export class StockTradingSummaryComponent {
  private _positions: PositionInstance[];

  positionGroups: PositionGroup[];
  totalCost: number;
  totalRiskedAmount: number;
  totalProfit: number;
  sortProperty: string;
  sortDirection: number;

  @Input()
  set positions(value:PositionInstance[]) {
    this._positions = value
    this.totalCost = this.sum(value, (p:PositionInstance) => p.averageCostPerShare * p.numberOfShares)
    this.totalRiskedAmount = this.sum(value, (p:PositionInstance) => p.costAtRiskBasedOnStopPrice)
    this.totalProfit = this.sum(value, (p:PositionInstance) => this.getUnrealizedProfit(p))
    this.positionGroups = this.breakdownByStrategy(value)
  }
  get positions():PositionInstance[] {
    return this._positions
  }

  @Input()
  quotes:Map<string, StockQuote>


  @Input()
  brokerageAccount: BrokerageAccount

  getStrategy(position:PositionInstance) : string {
    let strategy = position.labels.find(l => l.key == 'strategy')
    return strategy ? strategy.value : "none"
  }

  getUnrealizedProfit(position:PositionInstance) : number {
    let quote = this.quotes[position.ticker]
    return quote ? (quote.price - position.averageCostPerShare) * position.numberOfShares + position.profit : 0
  }

  getSortFunc(property:string) : (a:PositionGroup, b:PositionGroup) => number {
    switch (property) {
      case 'cost':
        return (a, b) => b.cost - a.cost
      case 'risk':
        return (a, b) => b.risk - a.risk
      case 'profit':
        return (a, b) => b.profit - a.profit
      case 'gain':
        return (a, b) => b.profit / b.cost - a.profit / a.cost
      default:
        return (a, b) => b.strategy.localeCompare(a.strategy)
    }
  }

  sort(property:string) {
    this.sortDirection = this.sortProperty == property ? 1 : -1

    this.sortProperty = property

    const sortFunc = this.getSortFunc(property)

    const adjustedFunc = (a:PositionGroup, b:PositionGroup) => this.sortDirection * sortFunc(a, b)

    this.positionGroups.sort(adjustedFunc)
  }

  breakdownByStrategy(positions:PositionInstance[]) : PositionGroup[] {
    console.log("breaking down positions")

    if (!positions) return []

    let strategyGroups = positions.reduce((acc, cur) => {
      let strategyKey = this.getStrategy(cur)

      if (!acc[strategyKey]) {
        acc[strategyKey] = []
      }

      acc[strategyKey].push(cur)

      return acc
    }, {})

    // custom groups
    strategyGroups["allbutlongterm"] = positions.filter(p => !isLongTermStrategy(this.getStrategy(p)))
    strategyGroups["long"] = positions.filter((p:PositionInstance) => p.isShort === false && !isLongTermStrategy(this.getStrategy(p)))
    strategyGroups["short"] = positions.filter((p:PositionInstance) => p.isShort === true && !isLongTermStrategy(this.getStrategy(p)))

    let groupsArray = []

    for (const key in strategyGroups) {
        let groupPositions = strategyGroups[key]

        let group = {
          strategy : key,
          positions,
          cost : this.sum(groupPositions, p => p.averageCostPerShare * p.numberOfShares),
          risk : this.sum(groupPositions, p => p.costAtRiskBasedOnStopPrice),
          profit : this.sum(groupPositions, p => this.getUnrealizedProfit(p)),
          length : groupPositions.length
        }

        groupsArray.push(group)
    }

    return groupsArray
  }

  private sum(positions, func) : number {
    return positions.reduce((acc, cur) => acc + func(cur), 0)
  }
}
