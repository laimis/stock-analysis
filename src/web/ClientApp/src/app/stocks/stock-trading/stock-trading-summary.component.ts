import { Component, Input } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';

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
    this.totalCost = this.sum(value, p => p.averageCostPerShare * p.numberOfShares)
    this.totalRiskedAmount = this.sum(value, p => p.costAtRiskedBasedOnStopPrice)
    this.totalProfit = this.sum(value, p => p.combinedProfit)
    this.positionGroups = this.breakdownByStrategy(value)
  }
  get positions():PositionInstance[] {
    return this._positions
  }

  getStrategy(position:PositionInstance) : string {
    let strategy = position.labels.find(l => l.key == 'strategy')
    return strategy ? strategy.value : "none"
  }

  getSortFunc(property) : (a:PositionGroup, b:PositionGroup) => number {
    switch (property) {
      case 'cost':
        return (a, b) => b.cost - a.cost
      case 'risk':
        return (a, b) => b.risk - a.risk
      case 'profit':
        return (a, b) => b.profit - a.profit
      default:
        return (a, b) => b.strategy.localeCompare(a.strategy)
    }
  }

  log(message) {
    alert(message)
  }

  sort(property:string) {
    this.sortDirection = this.sortProperty == property ? 1 : -1

    this.sortProperty = property
    
    var sortFunc = this.getSortFunc(property)

    var adjustedFunc = (a, b) => this.sortDirection * sortFunc(a, b)

    this.positionGroups.sort(adjustedFunc)
  }

  breakdownByStrategy(positions) : PositionGroup[] {
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

    // all all positions whose strategy is "shortterm"
    strategyGroups["allbutlongterm"] = positions.filter(p => this.getStrategy(p) !== "longterm")

    let groupsArray = []

    for (const key in strategyGroups) {
        let groupPositions = strategyGroups[key]

        let group = {
          strategy : key,
          positions,
          cost : this.sum(groupPositions, p => p.averageCostPerShare * p.numberOfShares),
          risk : this.sum(groupPositions, p => p.costAtRiskedBasedOnStopPrice),
          profit : this.sum(groupPositions, p => p.combinedProfit),
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
