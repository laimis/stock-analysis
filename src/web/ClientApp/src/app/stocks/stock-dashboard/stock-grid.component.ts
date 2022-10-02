import { Component } from '@angular/core';
import { StocksService, StockGridEntry } from '../../services/stocks.service';

@Component({
  selector: 'stock-grid',
  templateUrl: './stock-grid.component.html',
  styleUrls: ['./stock-grid.component.css']
})
export class StockGridComponent {

  ownership: StockGridEntry[]
  loaded: boolean = false
  sortColumn: string
  sortDirection: number = -1

	constructor(private service : StocksService){}

	ngOnInit(): void {
    console.log("loading grid")
    this.fetchGrid()
  }

	fetchGrid() {
		this.service.getStockGrid().subscribe(result => {
      this.ownership = result;
      this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
    });
  }

  getKeys() {
    return this.ownership[0].outcomes.map(o => o.key)
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

    this.ownership.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    return (a:StockGridEntry, b:StockGridEntry) => {
      var aVal = a.outcomes.find(o => o.key === column).value
      var bVal = b.outcomes.find(o => o.key === column).value

      return aVal - bVal
    }
  }
}
