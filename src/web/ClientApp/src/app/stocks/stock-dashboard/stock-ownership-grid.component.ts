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
      case "ticker":
        return (a:OwnedStock, b:OwnedStock) => a.ticker.localeCompare(b.ticker)
      case "currentPrice":
        return (a:OwnedStock, b:OwnedStock) => a.currentPrice - b.currentPrice
      case "averageCost":
        return (a:OwnedStock, b:OwnedStock) => a.averageCost - b.averageCost
      case "owned":
        return (a:OwnedStock, b:OwnedStock) => a.owned - b.owned
      case "equity":
        return (a:OwnedStock, b:OwnedStock) => a.equity - b.equity
      case "profits":
        return (a:OwnedStock, b:OwnedStock) => a.profits - b.profits
      case "profitsPct":
        return (a:OwnedStock, b:OwnedStock) => a.profitsPct - b.profitsPct
    }

    console.log("unrecognized sort column " + column)
    return null;
  }
}
