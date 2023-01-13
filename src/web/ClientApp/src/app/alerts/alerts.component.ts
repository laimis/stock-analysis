import { Component, Input, OnInit } from '@angular/core';
import { AlertsContainer, PriceMonitor, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css']
})

export class AlertsComponent implements OnInit {
  container: AlertsContainer;
  monitorGroups: PriceMonitor[][];

  constructor(private stockService : StocksService) { }

  @Input()
  hideUntriggered : boolean = false;

  @Input()
  hideRecentTriggered : boolean = false;

  ngOnInit(): void {
    this.stockService.getAlerts().subscribe(container => {
      this.container = container;

      // we want to group container.monitors into groups that are triggered/not triggered
      // and then triggered ones to group further by description, order by ticker

      var compareTickers = (a:PriceMonitor, b:PriceMonitor) => a.ticker.localeCompare(b.ticker)

      var notTriggered = container.monitors.filter(m => !m.triggeredAlert).sort(compareTickers);
      var triggered = container.monitors.filter(m => m.triggeredAlert).sort(compareTickers);

      var descriptions = new Set(triggered.map(m => m.description));
      
      var groups = []
      descriptions.forEach(description => {
        var group = triggered.filter(m => m.description === description).sort(compareTickers);
        groups.push(group);
      })

      if (!this.hideUntriggered) {
        groups.push(notTriggered);
      }

      this.monitorGroups = groups;
    });
  }

}
