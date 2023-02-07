import { Component } from '@angular/core';
import { StocksService, StockDetails, NoteList, OwnedOption, StockOwnership, StockProfile } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { charts_getTradingViewLink } from 'src/app/services/links.service';

@Component({
  selector: 'stock-details',
  templateUrl: './stock-details.component.html',
  styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent {

  profile: StockProfile
  ticker: string
	loaded: boolean = false
  stock: StockDetails
  notes: NoteList
  ownership: StockOwnership
  options: OwnedOption[]
  activeTab: string = ''

	constructor(
		private stocks : StocksService,
    private route: ActivatedRoute,
    private title: Title){}

	ngOnInit(): void {
		var ticker = this.route.snapshot.paramMap.get('ticker');
		if (ticker){
      this.ticker = ticker;
			this.fetchStock();
		}

    this.activeTab = this.route.snapshot.paramMap.get('tab') || 'stocks'
	}

	fetchStock() {
		this.stocks.getStockDetails(this.ticker).subscribe(result => {
      this.stock = result;
      this.profile = result.profile
      this.loaded = true;
      this.title.setTitle(this.stock.ticker + " - Nightingale Trading")
		}, error => {
			console.error(error);
			this.loaded = true;
    });

    this.stocks.getNotes(this.ticker).subscribe(result => {
      this.notes = result
    }, error => {
      console.error(error)
    })

    this.loadStockOwnership()

    this.loadOptionOwnership()
  }

  loadOptionOwnership() {
    this.stocks.getOwnedOptions(this.ticker).subscribe(result => {
      this.options = result
    })
  }

  loadStockOwnership() {
    this.stocks.getStockOwnership(this.ticker).subscribe(result => {
      this.ownership = result

      if (this.ownership != null)
      {
        this.activeTab = 'stocks'
      }
      else
      {
        this.activeTab = 'analysis'
      }
    })
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }

  stockOwnershipChanged(e) {
    this.loadStockOwnership()
  }

  optionOwnershipChanged(e) {
    this.loadOptionOwnership()
  }

  getTradingViewLink(ticker:string) {
    return charts_getTradingViewLink(ticker)
  }
}
