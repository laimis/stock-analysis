import { Component, Input } from '@angular/core';
import { StocksService, StockAnalysisEntry } from '../../services/stocks.service';

@Component({
  selector: 'stock-grid',
  templateUrl: './stock-grid.component.html',
  styleUrls: ['./stock-grid.component.css']
})
export class StockGridComponent {

  entries: StockAnalysisEntry[]
  loaded: boolean = false
  sortColumn: string
  sortDirection: number = -1

	constructor(private service : StocksService){}

  @Input()
  daily: boolean = false

	ngOnInit(): void {
    console.log("loading grid")
    this.fetchGrid()
  }

	fetchGrid() {
    var observable = this.daily ? this.service.getPortfolioDailyAnalysis() : this.service.getPortfolioAnalysis()
		observable.subscribe(result => {
      this.entries = result;
      this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
    });
  }

  getKeys() {
    return this.entries[0].outcomes.map(o => o.key)
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

    this.entries.sort(finalFunc)
  }

  private getSortFunc(column:string) {
    return (a:StockAnalysisEntry, b:StockAnalysisEntry) => {
      var aVal = a.outcomes.find(o => o.key === column).value
      var bVal = b.outcomes.find(o => o.key === column).value

      return aVal - bVal
    }
  }
}
