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

  newPricePoint       : number

  @Input()
  set alerts(alert: object) {
    this.alert = alert
  }
  get alerts(): object { return this.alert }

  @Input()
  public ticker: string;

  @Output()
  alertsChanged = new EventEmitter();

	constructor(private service: StocksService){}

  ngOnInit(): void {}

  clearFields() {
    this.newPricePoint = null
  }

  addPricePoint() {
    this.service.addAlert(this.ticker, this.newPricePoint).subscribe( r => {
      this.alertsChanged.emit("added")
      this.clearFields()
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }

  removeAlert(id:string) {
    this.service.removeAlert(this.ticker, id).subscribe( r => {
      this.alertsChanged.emit("removed")
      this.clearFields()
      this.success = true
    }, err => {
      this.errors = GetErrors(err)
    })
  }
}
