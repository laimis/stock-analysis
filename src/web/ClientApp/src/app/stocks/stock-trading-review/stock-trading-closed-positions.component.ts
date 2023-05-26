import { Component, Input } from '@angular/core';
import { PositionInstance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent {
  private _positions: PositionInstance[];
  tickers: string[];
  
  @Input()
  set positions(value: PositionInstance[]) {
    this._positions = value
    this.tickers = value
      .map(p => p.ticker)
      .filter((v, i, a) => a.indexOf(v) === i)
      .sort()
  }
  get positions(): PositionInstance[] {
    return this._positions
  }

  matchesFilter(position:PositionInstance) {
    
    if (this.tickerFilter != 'all' && !position.ticker.toLowerCase().includes(this.tickerFilter.toLowerCase())) {
      return false
    }

    if (this.gradeFilter != 'all' && position.grade != this.gradeFilter) {
      return false
    }

    var winMasmatch = position.profit >= 0 && this.outcomeFilter === 'loss'
    var lossMismatch = position.profit < 0 && this.outcomeFilter === 'win'

    if (this.outcomeFilter != 'all' && (winMasmatch || lossMismatch)) {
      return false
    }

    return true
  }

  sortColumn : string = 'closed'
  sortDirection : number = -1
  tickerFilter: string = 'all'
  gradeFilter: string = 'all'
  outcomeFilter: string = 'all'
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
}
