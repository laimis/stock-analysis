import { Component } from '@angular/core';
import { StocksService, StockSummary } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'stock-grid',
  templateUrl: './stock-grid.component.html',
  styleUrls: ['./stock-grid.component.css']
})
export class StockGridComponent {

  ownership: StockSummary[]
  loaded: boolean = false

	constructor(private stocks : StocksService){}

	ngOnInit(): void {
    this.fetchGrid()
  }

	fetchGrid() {
		this.stocks.getStockGrid().subscribe(result => {
      this.ownership = result;
      this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
    });
  }
}
