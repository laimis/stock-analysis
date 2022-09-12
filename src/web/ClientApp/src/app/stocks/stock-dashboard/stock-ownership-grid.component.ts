import { Component, OnInit, Input } from '@angular/core';
import { PositionInstance } from '../../services/stocks.service';


@Component({
  selector: 'stock-ownership-grid',
  templateUrl: './stock-ownership-grid.component.html',
  styleUrls: ['./stock-ownership-grid.component.css']
})
export class StockOwnershipGridComponent implements OnInit {

	public loaded : boolean = false;

  @Input() positions: PositionInstance[];
  @Input() selectedCategory: string

	ngOnInit() {}

  sortColumn : string
  sortDirection : number = -1

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
      case "averageCost":
        return (a:PositionInstance, b:PositionInstance) => a.averageCostPerShare - b.averageCostPerShare
      case "price":
        return (a:PositionInstance, b:PositionInstance) => a.price - b.price
      case "daysheld":
        return (a:PositionInstance, b:PositionInstance) => a.daysHeld - b.daysHeld
      case "invested":
        return (a:PositionInstance, b:PositionInstance) => a.cost - b.cost
      case "profits":
        return (a:PositionInstance, b:PositionInstance) => a.unrealizedProfit - b.unrealizedProfit
      case "equity":
        return (a:PositionInstance, b:PositionInstance) => a.cost + a.unrealizedProfit - b.cost - b.unrealizedProfit
      case "profitsPct":
        return (a:PositionInstance, b:PositionInstance) => a.unrealizedGainPct - b.unrealizedGainPct
      case "owned":
        return (a:PositionInstance, b:PositionInstance) => a.numberOfShares - b.numberOfShares
      case "ticker":
        return (a:PositionInstance, b:PositionInstance) => a.ticker.localeCompare(b.ticker)
      case "daysSinceLastTransaction":
        return (a:PositionInstance, b:PositionInstance) => a.daysSinceLastTransaction - b.daysSinceLastTransaction
    }

    console.log("unrecognized sort column " + column)
    return null;
  }

  ownershipPct(ticker:PositionInstance) {
    let total = this.positions
      .map(s => s.cost)
      .reduce((acc, curr) => acc + curr)

    return ticker.cost / total
  }

  equityPct(position:PositionInstance) {
    let total = this.positions
      .map(s => s.cost  + s.unrealizedProfit)
      .reduce((acc, curr) => acc + curr)

    return (position.cost + position.unrealizedProfit) / total
  }

  highlightRow(position:PositionInstance) {
    return (position.category == null && this.selectedCategory == "notset")
      || this.selectedCategory == position.category
  }
}
