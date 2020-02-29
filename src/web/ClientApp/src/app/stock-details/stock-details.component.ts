import { Component } from '@angular/core';
import { StocksService, StockSummary, NoteList } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'stock-details',
  templateUrl: './stock-details.component.html',
  styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent {

  profile: object
  ticker: string
	loaded: boolean = false
  stock: StockSummary
  notes: NoteList
  ownership: object
  activeTab: string = 'fundamentals'

	constructor(
		private stocks : StocksService,
		private route: ActivatedRoute){}

	ngOnInit(): void {
		var ticker = this.route.snapshot.paramMap.get('ticker');
		if (ticker){
			this.ticker = ticker;
			this.fetchStock();
		}
	}

	fetchStock() {
		this.stocks.getStockSummary(this.ticker).subscribe(result => {
      this.stock = result;
      this.profile = result.profile
			this.loaded = true;
		}, error => {
			console.error(error);
			this.loaded = true;
    });

    this.stocks.getNotes(this.ticker).subscribe(result => {
      this.notes = result
    }, error => {
      console.error(error)
    })

    this.stocks.getStock(this.ticker).subscribe(result => {
      this.ownership = result
    })
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }
}
