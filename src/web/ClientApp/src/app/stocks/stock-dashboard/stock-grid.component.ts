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
    switch(column) {
      case "stock":
        return (a:StockGridEntry, b:StockGridEntry) => a.ticker.localeCompare(b.ticker)
      case "price":
        return (a:StockGridEntry, b:StockGridEntry) => a.price - b.price
      case "pe":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.peRatio - b.stats.peRatio
      case "volume":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.avg30Volume - b.stats.avg30Volume
      case "marketCap":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.marketCap - b.stats.marketCap
      case "above50":
        return (a:StockGridEntry, b:StockGridEntry) => a.above50 - b.above50
      case "above200":
        return (a:StockGridEntry, b:StockGridEntry) => a.above200 - b.above200
      case "revenue":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.revenue - b.stats.revenue
      case "grossProfit":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.grossProfit - b.stats.grossProfit
      case "profitMargin":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.profitMargin - b.stats.profitMargin
      case "totalCash":
        return (a:StockGridEntry, b:StockGridEntry) => a.stats.totalCash - b.stats.totalCash
    }

    console.log("unrecognized sort column " + column)
    return null;
  }
}
