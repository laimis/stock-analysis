import {CurrencyPipe, DecimalPipe, PercentPipe} from '@angular/common';
import {AfterViewInit, Component, Input, OnDestroy, OnInit} from '@angular/core';
import {AlertsContainer, OutcomeValueTypeEnum, StockAlert, StocksService} from 'src/app/services/stocks.service';
import {toggleVisuallyHidden} from '../services/utils';


type StockAlertGroup = {
    identifier: string
    alerts: StockAlert[]
    expanded: boolean
}

@Component({
    selector: 'app-alerts',
    templateUrl: './alerts.component.html',
    styleUrls: ['./alerts.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class AlertsComponent implements OnInit, AfterViewInit, OnDestroy {
    container: AlertsContainer;
    alertGroups: StockAlertGroup[];
    intervalId: any;
    lastRefreshed: string;
    scheduled: boolean = false
    sortColumn = 'description'
    sortDirection = 'asc'
    recentlyTriggeredExpanded = false;

    @Input()
    hideRecentTriggered: boolean = false;
    @Input()
    hideMessages: boolean = false;
    @Input()
    hideScheduling: boolean = false;
    
    constructor(
        private stockService: StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
    }

    ngAfterViewInit() {
        this.turnOnRefresh();
    }

    private turnOnRefresh() {
        this.intervalId = setInterval(() => {
            this.fetchData()
        }, 5000);
    }
    
    private turnOffRefresh() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }

    ngOnInit(): void {
        this.fetchData();
    }

    ngOnDestroy() {
        this.turnOffRefresh();
    }

    fetchData() {
        this.stockService.getAlerts().subscribe(container => {
            this.container = container;

            const compareTickers = (a: StockAlert, b: StockAlert) => a.ticker.localeCompare(b.ticker);

            const sorted = container.alerts.sort(compareTickers);

            const identifiers = new Set(sorted.map(m => m.identifier));

            const groups = [];
            identifiers.forEach(identifier => {
                const alerts = sorted.filter(m => m.identifier === identifier).sort(compareTickers);
                const group = {
                    identifier: identifier,
                    alerts: alerts,
                    expanded: false
                }
                groups.push(group);
            })

            this.applySort(groups)

            this.alertGroups = groups
            this.lastRefreshed = new Date().toLocaleString();
            this.scheduled = false
        });
    }

    toggleVisibility(elem:HTMLElement) {
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

    sort(column: string, alertGroups: StockAlertGroup[] | null = null) {
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

    applySort(alertGroups: StockAlertGroup[]) {
        const compare = (a: StockAlert, b: StockAlert) => {
            if (this.sortDirection === 'asc') {
                return a[this.sortColumn] > b[this.sortColumn] ? 1 : -1
            } else {
                return a[this.sortColumn] < b[this.sortColumn] ? 1 : -1
            }
        };

        alertGroups.forEach(group => {
            group.alerts.sort(compare)
        })
    }
    
    toggleGroupExpansion(group: StockAlertGroup) {
        group.expanded = !group.expanded
        if (group.expanded) {
            this.turnOffRefresh()
        } else {
            this.turnOnRefresh()
        }
    }
}
