import {Component, OnInit, ViewChild} from '@angular/core';
import {
  StocksService,
  StockDetails,
  NoteList,
  OwnedOption,
  StockOwnership,
  StockProfile,
  BrokerageOrder
} from '../../services/stocks.service';
import { ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import {GetErrors} from "../../services/utils";
import {StockPositionsService} from "../../services/stockpositions.service";
import {BrokerageService} from "../../services/brokerage.service";

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
  orders: BrokerageOrder[]
  errors: string[]

	constructor(
		private stocks : StocksService,
    private stockPositions: StockPositionsService,
    private brokerage : BrokerageService,
    private route: ActivatedRoute,
    private title: Title){}

	ngOnInit(): void {
    this.route.params.subscribe(param => {
      const ticker = param['ticker']
      if (ticker){
        this.ticker = ticker;
        this.fetchStock();
      }

      this.activeTab = param['tab'] || 'stocks'
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
    this.stockPositions.getStockOwnership(this.ticker).subscribe(result => {
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

  refreshOrders() {
    this.brokerage.brokerageAccount().subscribe(
      a => this.orders = a.orders,
      e => {
        this.errors = GetErrors(e)
      }
    )
  }
}
