import { Component, OnInit } from '@angular/core';
import { PriceMonitor, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})

export class AlertsComponent implements OnInit {
  monitors: PriceMonitor[];

  constructor(private stockService : StocksService) { }

  ngOnInit(): void {
    this.stockService.getPriceMonitors().subscribe(monitors => {
      this.monitors = monitors;
    });
  }

}
