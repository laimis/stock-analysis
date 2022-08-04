import { Component, OnInit, Input } from '@angular/core';
import { OwnedStock } from '../../services/stocks.service';


@Component({
  selector: 'stock-ownership-grid',
  templateUrl: './stock-ownership-grid.component.html',
  styleUrls: ['./stock-ownership-grid.component.css']
})
export class StockOwnershipGridComponent implements OnInit {

	public loaded : boolean = false;

  @Input() owned: OwnedStock[];
  @Input() violations:string[] = []
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

    this.owned.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    switch(column) {
      case "averageCost":
        return (a:OwnedStock, b:OwnedStock) => a.averageCost - b.averageCost
      case "price":
        return (a:OwnedStock, b:OwnedStock) => a.price - b.price
      case "daysheld":
        return (a:OwnedStock, b:OwnedStock) => a.daysHeld - b.daysHeld
      case "invested":
        return (a:OwnedStock, b:OwnedStock) => a.cost - b.cost
      case "profits":
        return (a:OwnedStock, b:OwnedStock) => a.profits - b.profits
      case "equity":
        return (a:OwnedStock, b:OwnedStock) => a.equity - b.equity
      case "profitsPct":
        return (a:OwnedStock, b:OwnedStock) => a.profitsPct - b.profitsPct
      case "owned":
        return (a:OwnedStock, b:OwnedStock) => a.owned - b.owned
      case "ticker":
        return (a:OwnedStock, b:OwnedStock) => a.ticker.localeCompare(b.ticker)
      case "daysSinceLastTransaction":
        return (a:OwnedStock, b:OwnedStock) => a.daysSinceLastTransaction - b.daysSinceLastTransaction
    }

    console.log("unrecognized sort column " + column)
    return null;
  }

  ownershipPct(ticker:OwnedStock) {
    let total = this.owned
      .map(s => s.cost)
      .reduce((acc, curr) => acc + curr)

    return ticker.cost / total
  }

  equityPct(ticker:OwnedStock) {
    let total = this.owned
      .map(s => s.equity)
      .reduce((acc, curr) => acc + curr)

    return ticker.equity / total
  }

  highlightRow(ticker:OwnedStock) {
    return (ticker.category == null && this.selectedCategory == "notset")
      || this.selectedCategory == ticker.category
  }
}
