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
  
    positionGroups: PositionGroup[];
    
    sortProperty: string;
    sortDirection: number;
    longPositions: PositionInstance[];
    shortPositions: PositionInstance[];
    totalLongCost: number;
    totalShortCost: number;
    totalProfit: number;

    @Input()
    set positions(value: PositionInstance[]) {
        
        this.longPositions = value.filter(p => p.isShort === false)
        this.shortPositions = value.filter(p => p.isShort === true)
        
        this.totalLongCost = this.reduce(this.longPositions, (p: PositionInstance) => p.averageCostPerShare * p.numberOfShares)
        this.totalShortCost = this.reduce(this.shortPositions, (p: PositionInstance) => p.averageCostPerShare * p.numberOfShares)
        this.totalProfit = this.reduce(value, (p: PositionInstance) => this.getUnrealizedProfit(p))
        this.positionGroups = this.breakdownByStrategy(value)
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

    private brokerageSortColumn: string = 'ticker'
    private brokerageSortDirection: string = 'asc'
    sortBrokerageTable(column:string) {
        if (this.brokerageSortColumn == column) {
            this.brokerageSortDirection = this.brokerageSortDirection == 'asc' ? 'desc' : 'asc'
        } else {
            this.brokerageSortColumn = column
            this.brokerageSortDirection = 'asc'
        }

        this.brokerageAccount.stockPositions.sort((a, b) => {
            if (this.brokerageSortColumn === 'total') {
                const aTotal = a.quantity * a.averageCost;
                const bTotal = b.quantity * b.averageCost;

                if (this.brokerageSortDirection == 'asc') {
                    return aTotal > bTotal ? 1 : -1
                } else {
                    return aTotal < bTotal ? 1 : -1
                }
            } else {
                if (this.brokerageSortDirection == 'asc') {
                    return a[this.brokerageSortColumn] > b[this.brokerageSortColumn] ? 1 : -1
                } else {
                    return a[this.brokerageSortColumn] < b[this.brokerageSortColumn] ? 1 : -1
                }
            }
        })
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
          cost : this.reduce(groupPositions, (p:PositionInstance) => p.averageCostPerShare * p.numberOfShares),
          risk : this.reduce(groupPositions, (p:PositionInstance) => p.costAtRiskBasedOnStopPrice),
          profit : this.reduce(groupPositions, (p:PositionInstance) => this.getUnrealizedProfit(p)),
          length : groupPositions.length
        }

        groupsArray.push(group)
    }

    return groupsArray
  }

  private reduce(positions:PositionInstance[], func: (p: PositionInstance) => number) : number {
    return positions.reduce((acc, cur) => acc + func(cur), 0)
  }
}
