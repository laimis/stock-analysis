import { Component, OnInit } from '@angular/core';
import { StocksService, StockDetails, NoteList, OwnedOption, StockOwnership, StockProfile, SECFiling } from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

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
  recentFilings: SECFiling[]

	constructor(
		private stocks : StocksService,
    private route: ActivatedRoute,
    private title: Title){}

	ngOnInit(): void {
    const ticker = this.route.snapshot.paramMap.get('ticker');
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
      if (this.filings) {
        this.recentFilings = this.filings.filter(
          f => new Date(f.filingDate) > new Date(new Date().setDate(new Date().getDate() - 7))
        )
      }
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
    }, err => {
      console.error(err)
    })
  }

  loadStockOwnership() {
    this.stocks.getStockOwnership(this.ticker).subscribe(result => {
      this.ownership = result
    }, err => {
      console.error(err)
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
}
