import {CurrencyPipe, DecimalPipe, PercentPipe} from '@angular/common';
import {AfterViewInit, Component, Input, OnDestroy, OnInit} from '@angular/core';
import {AlertsContainer, OutcomeValueTypeEnum, StockAlert, StocksService} from 'src/app/services/stocks.service';
import {toggleVisuallyHidden} from '../services/utils';

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
    @Input()
    hideRecentTriggered: boolean = false;
    @Input()
    hideMessages: boolean = false;
    @Input()
    hideScheduling: boolean = false;
    scheduled: boolean = false
    sortColumn = 'description'
    sortDirection = 'asc'

    constructor(
        private stockService: StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
    }

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

            var compareTickers = (a: StockAlert, b: StockAlert) => a.ticker.localeCompare(b.ticker)

            var sorted = container.alerts.sort(compareTickers);

            var identifiers = new Set(sorted.map(m => m.identifier));

            var groups = []
            identifiers.forEach(identifier => {
                var group = sorted.filter(m => m.identifier === identifier).sort(compareTickers);
                groups.push(group);
            })

            this.applySort(groups)

            this.alertGroups = groups
            this.lastRefreshed = new Date().toLocaleString();
            this.scheduled = false
        });
    }

    toggleVisibility(elem) {
        toggleVisuallyHidden(elem);
    }

    scheduleRun() {
        this.stockService.scheduleAlertRun().subscribe(
            () => {
                console.log("scheduled")
                this.scheduled = true
            },
        );
    }

    getValue(value: number, valueType: string) {
        if (valueType === OutcomeValueTypeEnum.Percentage) {
            return this.percentPipe.transform(value, '1.0-2')
        } else if (valueType === OutcomeValueTypeEnum.Currency) {
            return this.currencyPipe.transform(value)
        } else {
            return this.decimalPipe.transform(value)
        }
    }

    sort(column: string, alertGroups: StockAlert[][] | null = null) {
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc'
        } else {
            this.sortColumn = column
            this.sortDirection = 'asc'
        }

        if (!alertGroups) {
            alertGroups = this.alertGroups
        }

        this.applySort(alertGroups)
    }

    applySort(alertGroups: StockAlert[][]) {
        var column = this.sortColumn
        var compare = (a: StockAlert, b: StockAlert) => {
            if (this.sortDirection === 'asc') {
                return a[column] > b[column] ? 1 : -1
            } else {
                return a[column] < b[column] ? 1 : -1
            }
        }

        alertGroups.forEach(group => {
            group.sort(compare)
        })
    }
}
