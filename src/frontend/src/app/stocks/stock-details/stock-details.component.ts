import { Component, OnInit, inject } from '@angular/core';
import {
    PendingStockPosition,
    PositionChartInformation,
    StockPosition,
    PriceFrequency,
    Prices,
    StockDetails,
    StockOwnership,
    StocksService, BrokerageAccount, SECFilings, SECFiling, Reminder
} from '../../services/stocks.service';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { GetErrors } from "../../services/utils";
import { StockPositionsService } from "../../services/stockpositions.service";
import { BrokerageService } from "../../services/brokerage.service";
import { catchError, tap } from "rxjs/operators";
import { concat } from "rxjs";
import { OptionPosition, OptionService } from "../../services/option.service";
import { StockTradingNewPositionComponent } from "../stock-trading/stock-trading-new-position.component";
import { StockLinkAndTradingviewLinkComponent } from "../../shared/stocks/stock-link-and-tradingview-link.component";
import { CurrencyPipe, DatePipe, DecimalPipe, NgClass } from "@angular/common";
import { BrokerageNewOrderComponent } from "../../brokerage/brokerage-new-order.component";
import { BrokerageOrdersComponent } from "../../brokerage/brokerage-orders.component";
import { StockOptionComponent } from "./stock-option.component";
import { StockFundamentalsComponent } from "./stock-fundamentals.component";
import { StockAnalysisComponent } from "./stock-analysis.component";
import { StockOwnershipComponent } from "./stock-ownership.component";
import { StockTransactionComponent } from "./stock-transaction.component";
import { TradingViewLinkComponent } from "../../shared/stocks/trading-view-link.component";
import { MarketCapPipe } from "../../services/marketcap.filter";
import { ReminderFormComponent } from '../../alerts/reminder-form.component';
import { ReminderListComponent } from '../../alerts/reminder-list.component';
import { SecFilingsTableComponent } from '../../shared/sec/sec-filings-table.component';

@Component({
    selector: 'app-stock-details',
    templateUrl: './stock-details.component.html',
    imports: [
    StockTradingNewPositionComponent,
    StockLinkAndTradingviewLinkComponent,
    CurrencyPipe,
    DatePipe,
    MarketCapPipe,
    DecimalPipe,
    BrokerageNewOrderComponent,
    BrokerageOrdersComponent,
    StockOptionComponent,
    StockFundamentalsComponent,
    StockAnalysisComponent,
    StockOwnershipComponent,
    StockTransactionComponent,
    NgClass,
    RouterLink,
    TradingViewLinkComponent,
    ReminderFormComponent,
    ReminderListComponent,
    SecFilingsTableComponent
],
    styleUrls: ['./stock-details.component.css']
})
export class StockDetailsComponent implements OnInit {
    private stocks = inject(StocksService);
    private optionService = inject(OptionService);
    private stockPositions = inject(StockPositionsService);
    private brokerage = inject(BrokerageService);
    private route = inject(ActivatedRoute);
    private title = inject(Title);


    ticker: string
    stock: StockDetails
    stockOwnership: StockOwnership
    currentPosition: StockPosition
    currentPositionChartInfo: PositionChartInformation
    pendingPosition: PendingStockPosition | null = null
    options: OptionPosition[]
    prices: Prices
    account: BrokerageAccount
    activeTab: string = ''
    startDate: string
    endDate: string
    secFilings: SECFilings | null = null
    reminders: Reminder[] = []
    showReminderForm = false
    editingReminder: Reminder | null = null

    loading = {
        stock: false,
        ownership: false,
        options: false,
        orders: false,
        pending: false,
        secFilings: false,
        reminders: false
    }

    errors = {
        stock: null,
        ownership: null,
        options: null,
        notes: null,
        orders: null,
        pending: null,
        secFilings: null,
        reminders: null
    }

    ngOnInit(): void {
        this.route.params.subscribe(param => {
            const ticker = param['ticker']
            if (ticker) {
                this.ticker = ticker;
                // set the startDate and endDate to come from the query params, if they exist
                // if they do not, default startDate to 365 days ago and endDate to today
                this.startDate = this.route.snapshot.queryParams['startDate'] || new Date(new Date().setDate(new Date().getDate() - 365)).toISOString().split('T')[0]
                this.endDate = this.route.snapshot.queryParams['endDate'] || new Date().toISOString().split('T')[0]

                this.loadData();
            } else {
                this.errors.stock = ['No ticker provided']
            }

            this.activeTab = param['tab'] || 'stocks'
        }, error => {
            this.errors.stock = GetErrors(error)
        })
    }

    loadData() {
        this.loading.stock = true;
        this.loading.ownership = true;
        this.loading.options = true;
        this.loading.orders = true;
        this.loading.pending = true;

        this.loadStockDetails()
        this.loadStockOwnership()
        this.loadOptionOwnership()
        this.loadOrders()
        this.loadPendingPosition()
    }

    loadOrders() {
        this.brokerage.brokerageAccount().subscribe(
            a => {
                this.loading.orders = false
                this.account = a
            },
            e => {
                this.loading.orders = false
                this.errors.orders = GetErrors(e)
            }
        )
    }

    pendingPositionCreated() {
        this.loadPendingPosition()
        this.loadOrders()
    }

    brokerageOrderEntered(_: string) {
        this.loadStockOwnership()
        this.loadOrders()
    }

    positionChanged() {
        this.loadStockOwnership()
    }

    loadPendingPosition() {
        this.stocks.getPendingStockPositions().subscribe(result => {
            let position = result.filter(p => p.ticker == this.ticker)[0]
            if (position) {
                this.pendingPosition = position
            }
            this.loading.pending = false
        }, error => {
            this.errors.pending = GetErrors(error)
            this.loading.pending = false
        })
    }

    loadStockDetails() {
        this.stocks.getStockDetails(this.ticker).subscribe(result => {
            this.loading.stock = false;
            this.stock = result;
            this.title.setTitle(this.stock.ticker + " - " + this.stock.quote.lastPrice + " - Nightingale Trading")
        }, error => {
            this.errors.stock = GetErrors(error)
            this.loading.stock = false;
        });
    }


    loadOptionOwnership() {
        this.optionService.getOptionPositionsForTicker(this.ticker).subscribe(result => {
            this.loading.options = false;
            this.options = result
        }, err => {
            this.loading.options = false;
            this.errors.options = GetErrors(err)
        })
    }

    loadStockOwnership() {
        let pricesPromise =
            this.stocks.getStockPricesForDates(this.ticker, PriceFrequency.Daily, this.startDate, this.endDate)
                .pipe(
                    tap(result => this.prices = result),
                    catchError(error => {
                        this.errors.stock = GetErrors(error)
                        return []
                    })
                )

        let ownershipPromise =
            this.stockPositions.getStockOwnership(this.ticker)
                .pipe(
                    tap(result => {
                        this.stockOwnership = result
                        this.currentPosition = result.positions.filter(p => p.isOpen)[0]
                        this.loading.ownership = false;
                    }),
                    catchError(err => {
                        this.loading.ownership = false;
                        this.errors.ownership = GetErrors(err)
                        return []
                    })
                )

        concat(pricesPromise, ownershipPromise).subscribe(() => {
            if (this.currentPosition && this.prices) {

                let buyOrders = this.account?.stockOrders?.filter(o => o.ticker == this.ticker && o.isBuyOrder).map(o => o.price)
                let sellOrders = this.account?.stockOrders?.filter(o => o.ticker == this.ticker && !o.isBuyOrder).map(o => o.price)

                this.currentPositionChartInfo = {
                    averageBuyPrice: this.currentPosition.averageCostPerShare,
                    stopPrice: this.currentPosition.stopPrice,
                    transactions: this.currentPosition.transactions,
                    markers: [],
                    prices: this.prices.prices,
                    ticker: this.currentPosition.ticker,
                    buyOrders: buyOrders,
                    sellOrders: sellOrders,
                    movingAverages: this.prices.movingAverages
                }
            }
        }
        )
    }

    isActive(tabName: string) {
        return tabName == this.activeTab
    }

    activateTab(tabName: string) {
        this.activeTab = tabName
        
        // Load SEC filings when the SEC tab is activated
        if (tabName === 'sec' && !this.secFilings) {
            this.loadSecFilings()
        }
        
        // Load reminders when the reminders tab is activated
        if (tabName === 'reminders') {
            this.loadReminders()
        }
    }

    loadSecFilings() {
        this.loading.secFilings = true
        this.stocks.getStockSECFilings(this.ticker).subscribe({
            next: result => {
                this.errors.secFilings = null
                this.secFilings = result
                this.loading.secFilings = false
            },
            error: err => {
                this.errors.secFilings = GetErrors(err)
                this.loading.secFilings = false
            }
        })
    }

    closePendingPosition(pendingPosition: PendingStockPosition) {
        this.stocks.closePendingPosition(pendingPosition.id, "cancelled").subscribe(
            {
                next: () => {
                    this.loadPendingPosition()
                    this.loadOrders()
                },
                error: err => {
                    this.errors.pending = GetErrors(err)
                }
            })
    }

    getPERatioClass(peRatio: number): string {
        if (!peRatio) return '';
        if (peRatio < 10) return 'metric-low';
        if (peRatio > 25) return 'metric-high';
        return 'metric-normal';
    }

    getPSRatioClass(psRatio: number): string {
        if (!psRatio) return '';
        if (psRatio < 1) return 'metric-low';
        if (psRatio > 5) return 'metric-high';
        return 'metric-normal';
    }

    loadReminders() {
        this.loading.reminders = true
        this.errors.reminders = null
        
        this.stocks.getReminders().subscribe({
            next: (reminders) => {
                // Filter reminders for this ticker
                this.reminders = reminders
                    .filter(r => r.ticker === this.ticker)
                    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
                this.loading.reminders = false
            },
            error: (error) => {
                this.errors.reminders = GetErrors(error)
                this.loading.reminders = false
            }
        })
    }

    toggleReminderForm() {
        this.showReminderForm = !this.showReminderForm
        if (!this.showReminderForm) {
            this.resetReminderForm()
        }
    }

    resetReminderForm() {
        this.editingReminder = null
        this.errors.reminders = null
    }

    onReminderSave(data: { date: string; message: string; ticker?: string }) {
        this.loading.reminders = true
        this.errors.reminders = null

        if (this.editingReminder) {
            // Update existing reminder
            this.stocks.updateReminder(
                this.editingReminder.reminderId,
                data.date,
                data.message,
                this.ticker,
                this.editingReminder.state
            ).subscribe({
                next: () => {
                    this.loadReminders()
                    this.toggleReminderForm()
                    this.loading.reminders = false
                },
                error: (error) => {
                    this.errors.reminders = GetErrors(error)
                    this.loading.reminders = false
                }
            })
        } else {
            // Create new reminder
            this.stocks.createReminder(
                data.date,
                data.message,
                this.ticker
            ).subscribe({
                next: () => {
                    this.loadReminders()
                    this.toggleReminderForm()
                    this.loading.reminders = false
                },
                error: (error) => {
                    this.errors.reminders = GetErrors(error)
                    this.loading.reminders = false
                }
            })
        }
    }

    editReminder(reminder: Reminder) {
        this.editingReminder = reminder
        this.showReminderForm = true
    }

    deleteReminder(reminder: Reminder) {
        this.loading.reminders = true
        this.errors.reminders = null

        this.stocks.deleteReminder(reminder.reminderId).subscribe({
            next: () => {
                this.loadReminders()
                this.loading.reminders = false
            },
            error: (error) => {
                this.errors.reminders = GetErrors(error)
                this.loading.reminders = false
            }
        })
    }
}
