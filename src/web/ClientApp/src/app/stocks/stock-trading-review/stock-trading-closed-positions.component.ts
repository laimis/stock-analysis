import { Component, Input } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';
import {stockClosedPositionExportLink} from "../../services/links.service";


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent {
  private _positions: PositionInstance[];
  tickers: string[];
  groupedByMonth: {month:string, positions:PositionInstance[], wins:PositionInstance[], losses:PositionInstance[]}[]
  strategies: string[] = []

  @Input()
  set positions(value: PositionInstance[]) {
    this._positions = value
    this.tickers = value
      .map(p => p.ticker)
      .filter((v, i, a) => a.indexOf(v) === i)
      .sort()
    let groupedByMonth = value.reduce((a, b) => {
      var key = b.closed.substring(0, 7)
      if (!a.has(key)) {
        a.set(key, [])
      }
      let arr = a.get(key)
      arr.push(b)
      return a
    }, new Map<string, PositionInstance[]>())

    let groupedByMonthArray = []
    groupedByMonth.forEach((value, key) => {
      groupedByMonthArray.push(
        {
          month:key,
          positions: value,
          wins: value.filter(p => p.profit >= 0),
          losses: value.filter(p => p.profit < 0)
        }
      )
    })
    this.groupedByMonth = groupedByMonthArray

    value.map(
        (s) => {
          let strategyLabel = s.labels.findIndex(l => l.key === 'strategy')
          if (strategyLabel === -1) {
            return null
          }
          return s.labels[strategyLabel].value
        }
      )
      .filter((v, i, a) => v !== null && a.indexOf(v) === i) // unique
      .sort()
      .forEach(s =>this.strategies.push(s))
  }

  get positions(): PositionInstance[] {
    return this._positions
  }

    getClosedPositionExportLink() {
        return stockClosedPositionExportLink()
    }

  matchesFilter(position:PositionInstance) {

    if (this.tickerFilter != 'all' && !position.ticker.toLowerCase().includes(this.tickerFilter.toLowerCase())) {
      return false
    }

    if (this.gradeFilter != 'all' && position.grade != this.gradeFilter) {
      return false
    }

    if (this.strategyFilter != 'all' && !this.matchesStrategyCheck(position, this.strategyFilter)) {
      return false
    }

    if (this.plFilter != 'all') {
      if (this.plFilter === 'plus150') {
        return position.profit >= 150
      } else if (this.plFilter === 'plus100') {
        return position.profit >= 100
      } else if (this.plFilter === 'minus50') {
        return position.profit < -50
      } else if (this.plFilter === 'minus100') {
        return position.profit < -100
      } else {
        console.log("unrecognized pl filter " + this.plFilter)
        return false
      }
    }

    let winMasmatch = position.profit >= 0 && this.outcomeFilter === 'loss'
    let lossMismatch = position.profit < 0 && this.outcomeFilter === 'win'

    return !(this.outcomeFilter != 'all' && (winMasmatch || lossMismatch));
  }

  sortColumn : string = 'closed'
  sortDirection : number = -1
  tickerFilter: string = 'all'
  gradeFilter: string = 'all'
  outcomeFilter: string = 'all'
  plFilter: string = 'all'
  strategyFilter: string = 'all'
  showNotes: number = -1

  toggleShowNotes(index:number) {
    if (this.showNotes == index) {
      this.showNotes = -1
    } else {
      this.showNotes = index
    }
  }

  filterByTickerChanged(value:string) {
    this.tickerFilter = value
  }

  filterByGradeChanged(value:string) {
    this.gradeFilter = value
  }

  filterByOutcomeChanged(value:string) {
    this.outcomeFilter = value
  }

  filterByPLChanged(value:string) {
    this.plFilter = value
  }

  filterByStrategyChanged(value:string) {
    this.strategyFilter = value
  }

  matchesStrategyCheck(p:PositionInstance, strategy:string) {
    return p.labels.findIndex(l => l.key === "strategy" && l.value === strategy) !== -1
  }

  // ugly name, but essentially this returns a property for grouping
  // trades, and it's either closed date or open date of a position right now, otherwise, no grouping is
  // returned as we want no separator in that case (ie, if sorted by gain, grouping those by month makes no sense)
  getPropertyForSeperatorGrouping(position:PositionInstance) {
    var groupingInput = ""
    if (this.sortColumn === 'opened') {
      groupingInput = position.opened
    }
    if (this.sortColumn === 'closed') {
      groupingInput = position.closed
    }

    if (groupingInput === "") {
      return ""
    }

    return groupingInput.substring(0, 7)
  }

  getPositionsForMonth(month:string) {
    return this.positions.filter(p => this.getPropertyForSeperatorGrouping(p) === month)
  }

  getRRSumForMonth(position:PositionInstance) {
    var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
    return positions.reduce((a, b) => a + b.rr, 0)
  }

  getProfitSumForMonth(position:PositionInstance) {
    var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
    return this.getProfitSum(positions)
  }

  getProfitSum(positions:PositionInstance[]) {
    return positions.reduce((a, b) => a + b.profit, 0)
  }

  getTradeCountByGradeForMonth(position:PositionInstance, grade:string) {
    var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
    return positions.filter(p => p.grade === grade).length
  }

  getTradeCountForMonth(position:PositionInstance) {
    var positions = this.getPositionsForMonth(this.getPropertyForSeperatorGrouping(position))
    return positions.length
  }

  sort(column:string) {
    var func = this.getSortFunc(column);

    if (this.sortColumn != column) {
      this.sortDirection = -1
    } else {
      this.sortDirection *= -1
    }
    this.sortColumn = column

    var finalFunc = (a, b) => {
      var result = func(a, b)
      return result * this.sortDirection
    }

    this.positions.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    switch(column) {
      case "daysHeld":
        return (a:PositionInstance, b:PositionInstance) => a.daysHeld - b.daysHeld
      case "opened":
        return (a:PositionInstance, b:PositionInstance) => a.opened.localeCompare(b.opened)
      case "closed":
        return (a:PositionInstance, b:PositionInstance) => a.closed.localeCompare(b.closed)
      case "rr":
        return (a:PositionInstance, b:PositionInstance) => a.rr - b.rr
      case "profit":
        return (a:PositionInstance, b:PositionInstance) => a.profit - b.profit
      case "gainPct":
        return (a:PositionInstance, b:PositionInstance) => a.gainPct - b.gainPct
      case "grade":
        return (a:PositionInstance, b:PositionInstance) => (a.grade ?? "").localeCompare((b.grade ?? ""))
      case "ticker":
        return (a:PositionInstance, b:PositionInstance) => a.ticker.localeCompare(b.ticker)
    }

    console.log("unrecognized sort column " + column)
    return null;
  }

  LAYOUT_OPTION_TABLE:string = 'table'
  LAYOUT_OPTION_SPLIT_OUTCOME:string = 'splitoutcome'
  layout: string = this.LAYOUT_OPTION_TABLE

  toggleLayout() {
    if (this.layout === this.LAYOUT_OPTION_TABLE) {
      this.layout = this.LAYOUT_OPTION_SPLIT_OUTCOME
    } else {
      this.layout = this.LAYOUT_OPTION_TABLE
    }
  }
}
