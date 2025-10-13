import {CurrencyPipe, DatePipe, DecimalPipe, NgClass, PercentPipe} from '@angular/common';
import { AfterViewInit, Component, Input, OnDestroy, OnInit, inject } from '@angular/core';
import {AlertsContainer, OutcomeValueTypeEnum, StockAlert, StocksService} from 'src/app/services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from '../services/utils';
import { ErrorDisplayComponent } from "../shared/error-display/error-display.component";
import { StockLinkAndTradingviewLinkComponent } from "../shared/stocks/stock-link-and-tradingview-link.component";
import { RouterLink } from '@angular/router';


type StockAlertGroup = {
    identifier: string
    alerts: StockAlert[]
    expanded: boolean
}

@Component({
    selector: 'app-alerts',
    templateUrl: './alerts.component.html',
    styleUrls: ['./alerts.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe],
    imports: [ErrorDisplayComponent, DatePipe, NgClass, StockLinkAndTradingviewLinkComponent, RouterLink]
})
export class AlertsComponent implements OnInit, AfterViewInit, OnDestroy {
    private stockService = inject(StocksService);
    private percentPipe = inject(PercentPipe);
    private currencyPipe = inject(CurrencyPipe);
    private decimalPipe = inject(DecimalPipe);

    container: AlertsContainer;
    alertGroups: StockAlertGroup[];
    intervalId: any;
    lastRefreshed: string;
    scheduled: boolean = false
    sortColumn = 'description'
    sortDirection = 'asc'
    selectedSourceList = 'All'
    recentlyTriggeredExpanded = false;
    sourceLists: string[] = []
    errors: string[] = null
    private expandedIdentifiers = new Set<string>();

    @Input()
    hideRecentTriggered: boolean = false;
    @Input()
    hideMessages: boolean = false;
    @Input()
    hideScheduling: boolean = false;

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
            const uniqueSourceLists = new Set<string>() //[].concat(container.alerts.map(a => a.sourceLists)))
            container.alerts.forEach(a => a.sourceLists.forEach(s => uniqueSourceLists.add(s)))
            this.sourceLists = Array.from(uniqueSourceLists)
            this.createGroups();
        }, error => {
            this.errors = GetErrors(error)
        });
    }
    createGroups() {
        const compareTickers = (a: StockAlert, b: StockAlert) => a.ticker.localeCompare(b.ticker);

        const alertsOfInterest =
            this.container.alerts.filter(a => this.selectedSourceList === 'All' || a.sourceLists.includes(this.selectedSourceList))
                .sort(compareTickers);

        const identifiers = new Set(alertsOfInterest.map(m => m.identifier));

        const groups = [];
        identifiers.forEach(identifier => {
            const alerts = alertsOfInterest.filter(m => m.identifier === identifier).sort(compareTickers);
            const group = {
                identifier: identifier,
                alerts: alerts,
                expanded: this.expandedIdentifiers.has(identifier) || this.selectedSourceList !== 'All'
            }
            groups.push(group);
        });

        groups.sort((a, b) => a.identifier.localeCompare(b.identifier));

        this.applySort(groups);

        this.alertGroups = groups;
        this.lastRefreshed = new Date().toLocaleString();
        this.scheduled = false;
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
            error => {
                this.errors = GetErrors(error)
            }
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
            this.expandedIdentifiers.add(group.identifier);
            this.turnOffRefresh()
        } else {
            this.expandedIdentifiers.delete(group.identifier);
            this.turnOnRefresh()
        }
    }
    
    sourceListSelection(event:any) {
        this.selectedSourceList = event.target.value;
        this.expandedIdentifiers.clear(); // Clear expanded states when changing source list
        this.createGroups();
    }
}
