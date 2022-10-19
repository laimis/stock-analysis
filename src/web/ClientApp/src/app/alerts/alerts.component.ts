import { Component, OnInit } from '@angular/core';
import { AlertsContainer, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})

export class AlertsComponent implements OnInit {
  container: AlertsContainer;

  constructor(private stockService : StocksService) { }

  ngOnInit(): void {
    this.stockService.getAlerts().subscribe(container => {
      this.container = container;
    });
  }

}
