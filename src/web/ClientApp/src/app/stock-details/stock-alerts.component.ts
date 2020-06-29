import { Component, Input } from '@angular/core';
import { StockSummary } from '../services/stocks.service';

@Component({
  selector: 'stock-alerts',
  templateUrl: './stock-alerts.component.html',
  styleUrls: ['./stock-alerts.component.css']
})

export class StockAlertsComponent {

  public alert: object

  @Input()
  set stock(alert: object) {
    this.alert = alert
  }
  get stock(): object { return this.alert }

	constructor(){}

	ngOnInit(): void {}
}
