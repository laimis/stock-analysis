import { Component, Input, Output, EventEmitter } from '@angular/core';
import { StockDetails, StocksService, GetErrors, AlertLabelValue } from '../../services/stocks.service';

@Component({
  selector: 'stock-alerts',
  templateUrl: './stock-alerts.component.html',
  styleUrls: ['./stock-alerts.component.css']
})

export class StockAlertsComponent {

  alert             : any

  errors            : string[]
  success           : boolean

  summary           : StockDetails
  owned : any

  newPricePoint :number
  description: string

  alertBands : number[]
  priceBasedAlerts  : AlertLabelValue[]
  costBasedAlerts   : AlertLabelValue[]

  showShortcuts : boolean = false

  @Input()
  set alerts(alert: object) {
    this.alert = alert
  }
  get alerts(): object { return this.alert }

  @Input()
  set stock(stock: StockDetails) {
    this.summary = stock
    this.updatePriceBasedPoints()
  }
  get stock(): StockDetails { return this.summary}

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

	constructor(private service: StocksService){
    this.alertBands = new Array<number>()
    this.alertBands.push(1,2,3,4,5,6,7,8,9,10,15,20,25,30,50,75,90,100)
  }

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

    for (let index = this.alertBands.length - 1; index >= 0; index--) {

      var bandVal = this.alertBands[index]

      var val = (base + base * bandVal / 100.0).toFixed(2)
      var label = "+" + bandVal + "% " + val

      arr.push( new AlertLabelValue(label, val) )
    }

    arr.push(
      new AlertLabelValue(
        base.toString(),
        base.toString()
      )
    )

    for (let index = 0; index < this.alertBands.length; index++) {

      var bandVal = this.alertBands[index]

      var val = (base - base * bandVal / 100.0).toFixed(2)
      var label = "-" + bandVal + "% " + val

      arr.push( new AlertLabelValue(label, val) )
    }

    return arr;
  }

  addPricePoint(value:number) {

    value = Number(value)
    if (!value)
    {
      value = 0
    }

    console.log("desc: " + this.description)

    this.service.addAlert(this.ticker, this.description, value).subscribe( r => {
      this.alertsChanged.emit("added")
      this.success = true
      this.errors = null
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  removeAlert(id:string) {
    this.service.removeAlert(this.ticker, id).subscribe( r => {
      this.alertsChanged.emit("removed")
      this.success = true
      this.errors = null
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
