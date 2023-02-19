import { Component, Input } from '@angular/core';
import { PositionInstance, TradingStrategyPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'app-stock-trading-closed-positions',
  templateUrl: './stock-trading-closed-positions.component.html',
  styleUrls: ['./stock-trading-closed-positions.component.css']
})
export class StockTradingClosedPositionsComponent {
  performances: TradingStrategyPerformance[];

  @Input()
  positions: PositionInstance[]

  sortColumn : string
  sortDirection : number = -1
  timeFilter: string
  gradeFilter: string
  showNotes: number = -1

  toggleShowNotes(index:number) {
    if (this.showNotes == index) {
      this.showNotes = -1
    } else {
      this.showNotes = index
    }
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
