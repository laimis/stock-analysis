import { Component, Input, Output, EventEmitter } from '@angular/core';
import { StockSummary, StocksService, GetErrors } from '../services/stocks.service';

@Component({
  selector: 'stock-alerts',
  templateUrl: './stock-alerts.component.html',
  styleUrls: ['./stock-alerts.component.css']
})

export class StockAlertsComponent {

  alert               : any

  errors              : string[]
  success             : boolean

  summary               : StockSummary
  priceMinus20: number;
  priceMinus10: number;
  pricePlus10: number;
  pricePlus20: number;

  owned : any
  costMinus20: number;
  costMinus10: number;
  costPlus10: any;
  costPlus20: any;

  @Input()
  set alerts(alert: object) {
    this.alert = alert
  }
  get alerts(): object { return this.alert }

  @Input()
  set stock(stock: StockSummary) {
    this.summary = stock
    this.updatePriceBasedPoints()
  }
  get stock(): StockSummary { return this.summary}

  @Input()
  set ownership(ownership: any) {
    this.owned = ownership
    this.updateCostBasedPoints()
  }
  get ownership(): any { return this.owned}

  @Input()
  public ticker: string;

  @Output()
  alertsChanged = new EventEmitter();

	constructor(private service: StocksService){}

  ngOnInit(): void {}

  updatePriceBasedPoints() {
    this.priceMinus20 = this.stock.price - this.stock.price * 0.2
    this.priceMinus10 = this.stock.price - this.stock.price * 0.1
    this.pricePlus10 = this.stock.price + this.stock.price * 0.1
    this.pricePlus20 = this.stock.price + this.stock.price * 0.2
  }

  updateCostBasedPoints() {
    if (!this.owned) { return }
    this.costMinus20 = this.owned.averageCost - this.owned.averageCost * 0.2
    this.costMinus10 = this.owned.averageCost - this.owned.averageCost * 0.1
    this.costPlus10 = this.owned.averageCost + this.owned.averageCost * 0.1
    this.costPlus20 = this.owned.averageCost + this.owned.averageCost * 0.2
  }

  addPricePoint(value:number) {
    this.service.addAlert(this.ticker, value).subscribe( r => {
      this.alertsChanged.emit("added")
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  removeAlert(id:string) {
    this.service.removeAlert(this.ticker, id).subscribe( r => {
      this.alertsChanged.emit("removed")
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
