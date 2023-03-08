import { Component, OnInit } from '@angular/core';
import { StocksService, StockDetails, NoteList, OwnedOption, StockOwnership, StockProfile, SECFiling } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { charts_getTradingViewLink } from 'src/app/services/links.service';

@Component({
  selector: 'app-stock-details',
  templateUrl: './stock-details.component.html',
  styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent implements OnInit {

  profile: StockProfile
  ticker: string
	loaded: boolean = false
  stock: StockDetails
  notes: NoteList
  ownership: StockOwnership
  options: OwnedOption[]
  activeTab: string = ''
  filings: SECFiling[]

	constructor(
		private stocks : StocksService,
    private route: ActivatedRoute,
    private title: Title){}

	ngOnInit(): void {
		var ticker = this.route.snapshot.paramMap.get('ticker');
		if (ticker){
      this.ticker = ticker;
			this.fetchStock();
      this.fetchSecFilings();
		}

    this.activeTab = this.route.snapshot.paramMap.get('tab') || 'stocks'
	}

  fetchSecFilings() {
    this.stocks.getStockSECFilings(this.ticker).subscribe(result => {
      this.filings = result.filings
    }, error => {
      console.error(error)
    })
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
