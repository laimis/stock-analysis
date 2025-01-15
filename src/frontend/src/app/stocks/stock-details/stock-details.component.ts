import {Component, OnInit} from '@angular/core';
import {
    BrokerageStockOrder,
    PendingStockPosition,
    PositionChartInformation,
    PositionInstance,
    PriceFrequency,
    Prices,
    StockDetails,
    StockOwnership,
    StocksService
} from '../../services/stocks.service';
import {ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {GetErrors} from "../../services/utils";
import {StockPositionsService} from "../../services/stockpositions.service";
import {BrokerageService} from "../../services/brokerage.service";
import {catchError, tap} from "rxjs/operators";
import {concat} from "rxjs";
import {OptionPosition, OptionService} from "../../services/option.service";

@Component({
    selector: 'app-stock-details',
    templateUrl: './stock-details.component.html',
    styleUrls: ['./stock-details.component.css'],
    standalone: false
})
export class StockDetailsComponent implements OnInit {

    ticker: string
    stock: StockDetails
    stockOwnership: StockOwnership
    currentPosition: PositionInstance
    currentPositionChartInfo: PositionChartInformation
    pendingPosition: PendingStockPosition|null = null
    options: OptionPosition[]
    prices: Prices
    orders: BrokerageStockOrder[]
    activeTab: string = ''
    startDate: string
    endDate: string

    loading = {
        stock: false,
        ownership: false,
        options: false,
        orders: false,
        pending: false
    }

    errors = {
        stock: null,
        ownership: null,
        options: null,
        notes: null,
        orders: null,
        pending: null
    }

    constructor(
        private stocks: StocksService,
        private optionService: OptionService,
        private stockPositions: StockPositionsService,
        private brokerage: BrokerageService,
        private route: ActivatedRoute,
        private title: Title) {
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
                this.orders = a.stockOrders
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
            this.title.setTitle(this.stock.ticker + " - Nightingale Trading")
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
                    
                    
                    let buyOrders = this.orders?.filter(o => o.ticker == this.ticker && o.isBuyOrder).map(o => o.price)
                    let sellOrders = this.orders?.filter(o => o.ticker == this.ticker && !o.isBuyOrder).map(o => o.price)
                    
                    this.currentPositionChartInfo = {
                        averageBuyPrice: this.currentPosition.averageCostPerShare,
                        stopPrice: this.currentPosition.stopPrice,
                        transactions: this.currentPosition.transactions,
                        markers: [],
                        prices: this.prices,
                        ticker: this.currentPosition.ticker,
                        buyOrders: buyOrders,
                        sellOrders: sellOrders
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
}
