import {Component, OnInit} from '@angular/core';
import {
  StocksService,
  StockDetails,
  OwnedOption,
  StockOwnership,
  BrokerageOrder, PositionInstance, stocktransactioncommand
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

  ticker: string
	stock: StockDetails
  stockOwnership: StockOwnership
  currentPosition: PositionInstance
  options: OwnedOption[]
  orders: BrokerageOrder[]
  activeTab: string = ''

  loading = {
    stock: false,
    ownership: false,
    options: false,
    orders: false
  }

  errors = {
    stock: null,
    ownership: null,
    options: null,
    notes: null,
    orders: null
  }

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
        this.loadData();
      } else {
        this.errors.stock = ['No ticker provided']
      }

      this.activeTab = param['tab'] || 'trade'
    })
	}

	loadData() {
    this.loading.stock = true;
    this.loading.ownership = true;
    this.loading.options = true;
    this.loading.orders = true;

    this.loadStockDetails()
    this.loadStockOwnership()
    this.loadOptionOwnership()
    this.loadOrders()
  }

  loadOrders() {
    this.brokerage.brokerageAccount().subscribe(
      a => {
        this.loading.orders = false
        this.orders = a.orders
      },
      e => {
        this.loading.orders = false
        this.errors.orders = GetErrors(e)
      }
    )
  }

  purchaseRequested(command:stocktransactioncommand) {
    this.stockPositions.purchase(command).subscribe(
      () => {
        this.loading.ownership = true;
        this.loadStockOwnership()
      },
      error => {
        this.errors.notes = GetErrors(error)
      }
    )
  }

  sellRequested(command:stocktransactioncommand) {
    this.stockPositions.sell(command).subscribe(
      () => {
        this.loading.ownership = true;
        this.loadStockOwnership()
      },
      error => {
        this.errors.notes = GetErrors(error)
      }
    )
  }

  loadStockDetails() {
    this.stocks.getStockDetails(this.ticker).subscribe(result => {
      this.loading.stock = false;
      this.stock = result;
      this.title.setTitle(this.stock.ticker + " - Nightingale Trading")
    }, error => {
      this.errors.stock = GetErrors(error)
      this.loading.stock = false;
    });
  }


    loadOptionOwnership() {
    this.stocks.getOwnedOptions(this.ticker).subscribe(result => {
      this.loading.options = false;
      this.options = result
    }, err => {
      this.loading.options = false;
      this.errors.options = GetErrors(err)
    })
  }

  loadStockOwnership() {
    this.stockPositions.getStockOwnership(this.ticker).subscribe(result => {
      this.stockOwnership = result
      this.currentPosition = result.positions.filter(p => p.isOpen)[0]
      this.loading.ownership = false;
    }, err => {
      this.loading.ownership = false;
      this.errors.ownership = GetErrors(err)
    })
  }

  isActive(tabName:string) {
    return tabName == this.activeTab
  }

  activateTab(tabName:string) {
    this.activeTab = tabName
  }
}
