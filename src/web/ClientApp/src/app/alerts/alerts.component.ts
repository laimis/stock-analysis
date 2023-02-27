import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';
import { AfterViewInit, Component, Input, OnInit, OnDestroy } from '@angular/core';
import { AlertsContainer, OutcomeValueTypeEnum, StockAlert, StocksService } from 'src/app/services/stocks.service';
import { charts_getTradingViewLink } from '../services/links.service';
import { toggleVisuallyHidden } from '../services/utils';

@Component({
  selector: 'app-alerts',
  templateUrl: './alerts.component.html',
  styleUrls: ['./alerts.component.css'],
  providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})

export class AlertsComponent implements OnInit, AfterViewInit, OnDestroy {
  container: AlertsContainer;
  alertGroups: StockAlert[][];
  intervalId: any;
  lastRefreshed: string;

  constructor(
    private stockService : StocksService,
    private percentPipe: PercentPipe,
    private currencyPipe: CurrencyPipe,
    private decimalPipe: CurrencyPipe
  ) { }

  @Input()
  hideRecentTriggered : boolean = false;

  @Input()
  hideMessages : boolean = false;

  @Input()
  hideScheduling : boolean = false;

  ngAfterViewInit() {
    this.intervalId = setInterval(() => {
      this.fetchData()
    }, 5000);
  }

  ngOnInit(): void {
    this.fetchData();
  }

  ngOnDestroy() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  fetchData() {
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
      this.lastRefreshed = new Date().toLocaleString();
      this.scheduled = false
    });
  }

  toggleVisibility(elem){
    toggleVisuallyHidden(elem);
  }

  scheduled:boolean = false
  scheduleRun() {
    this.stockService.scheduleAlertRun().subscribe(
      () => {
      console.log("scheduled")
      this.scheduled = true
      },
    );
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
      return this.decimalPipe.transform(value)
    }
  }
    

}
