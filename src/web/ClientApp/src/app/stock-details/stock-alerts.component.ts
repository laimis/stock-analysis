import { Component, Input, Output, EventEmitter } from '@angular/core';
import { StockSummary, StocksService, GetErrors, AlertLabelValue } from '../services/stocks.service';
import { timeStamp } from 'console';

@Component({
  selector: 'stock-alerts',
  templateUrl: './stock-alerts.component.html',
  styleUrls: ['./stock-alerts.component.css']
})

export class StockAlertsComponent {

  alert             : any

  errors            : string[]
  success           : boolean

  summary           : StockSummary
  owned : any

  priceBasedAlerts  : AlertLabelValue[]
  costBasedAlerts   : AlertLabelValue[]

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
    this.priceBasedAlerts = this.generateAlertBands(this.stock.price)
  }

  updateCostBasedPoints() {
    if (!this.owned) { return }

    this.costBasedAlerts = this.generateAlertBands(Number(this.owned.averageCost.toFixed(2)))

  }

  generateAlertBands(base : number) : Array<AlertLabelValue> {
    var arr = Array<AlertLabelValue>(0)

    for (let index = 100; index >= 1; index -= 2) {

      var val = (base + base * index / 100.0).toFixed(2)
      var label = "+" + index + "% " + val

      arr.push( new AlertLabelValue(label, val) )
    }

    arr.push(
      new AlertLabelValue(
        base.toString(),
        base.toString()
      )
    )

    for (let index = 1; index <= 100; index += 2) {

      var val = (base - base * index / 100.0).toFixed(2)
      var label = "-" + index + "% " + val

      arr.push( new AlertLabelValue(label, val) )
    }

    return arr;
  }

  addPricePoint(value:number) {

    value = Number(value)

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
