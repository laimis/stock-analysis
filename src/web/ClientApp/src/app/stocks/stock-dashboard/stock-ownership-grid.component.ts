import { Component, OnInit, Input } from '@angular/core';
import { StockTradingPosition } from '../../services/stocks.service';


@Component({
  selector: 'stock-ownership-grid',
  templateUrl: './stock-ownership-grid.component.html',
  styleUrls: ['./stock-ownership-grid.component.css']
})
export class StockOwnershipGridComponent implements OnInit {

	public loaded : boolean = false;

  @Input() positions: StockTradingPosition[];
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
        return (a:StockTradingPosition, b:StockTradingPosition) => a.averageCost - b.averageCost
      case "price":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.price - b.price
      case "daysheld":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.daysHeld - b.daysHeld
      case "invested":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.cost - b.cost
      case "profits":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.unrealizedGain - b.unrealizedGain
      case "equity":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.cost + a.unrealizedGain - b.cost - b.unrealizedGain
      case "profitsPct":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.unrealizedGainPct - b.unrealizedGainPct
      case "owned":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.numberOfShares - b.numberOfShares
      case "ticker":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.ticker.localeCompare(b.ticker)
      case "daysSinceLastTransaction":
        return (a:StockTradingPosition, b:StockTradingPosition) => a.daysSinceLastTransaction - b.daysSinceLastTransaction
    }

    console.log("unrecognized sort column " + column)
    return null;
  }

  ownershipPct(ticker:StockTradingPosition) {
    let total = this.positions
      .map(s => s.cost)
      .reduce((acc, curr) => acc + curr)

    return ticker.cost / total
  }

  equityPct(position:StockTradingPosition) {
    let total = this.positions
      .map(s => s.cost  + s.unrealizedGain)
      .reduce((acc, curr) => acc + curr)

    return (position.cost + position.unrealizedGain) / total
  }

  highlightRow(position:StockTradingPosition) {
    return (position.category == null && this.selectedCategory == "notset")
      || this.selectedCategory == position.category
  }
}
