import { CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { AlertsContainer, OutcomeValueTypeEnum, StockAlert, StocksService } from 'src/app/services/stocks.service';
import { charts_getTradingViewLink } from '../services/links.service';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css'],
  providers: [PercentPipe, CurrencyPipe]
})

export class AlertsComponent implements OnInit {
  container: AlertsContainer;
  alertGroups: StockAlert[][];

  constructor(
    private stockService : StocksService,
    private percentPipe: PercentPipe,
    private currencyPipe: CurrencyPipe
  ) { }

  @Input()
  hideRecentTriggered : boolean = false;

  @Input()
  hideMessages : boolean = false;

  ngOnInit(): void {
    this.stockService.getAlerts().subscribe(container => {
      this.container = container;

      var compareTickers = (a:StockAlert, b:StockAlert) => a.ticker.localeCompare(b.ticker)

      var sorted = container.alerts.sort(compareTickers);

      var descriptions = new Set(sorted.map(m => m.description));
      
      var groups = []
      descriptions.forEach(description => {
        var group = sorted.filter(m => m.description === description).sort(compareTickers);
        groups.push(group);
      })

      groups.sort((a,b) => a[0].description.localeCompare(b[0].description));

      this.alertGroups = groups
    });
  }

  getTradingViewLink(ticker:string){
    return charts_getTradingViewLink(ticker)
  }

  getValue(value: number,valueType: string) {
    if (valueType === OutcomeValueTypeEnum.Percentage) {
      return this.percentPipe.transform(value, '1.0-2')
    } else if (valueType === OutcomeValueTypeEnum.Currency) {
      return this.currencyPipe.transform(value)
    } else {
      return value
    }
  }
    

}
