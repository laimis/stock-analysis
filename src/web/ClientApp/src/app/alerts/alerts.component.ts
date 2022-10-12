import { Component, OnInit } from '@angular/core';
import { StockAlert, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})

export class AlertsComponent implements OnInit {
  alerts: StockAlert[];

  constructor(private stockService : StocksService) { }

  ngOnInit(): void {
    this.stockService.getTriggeredAlerts().subscribe(alerts => {
      this.alerts = alerts;
    });
  }

}
