import {Component, EventEmitter, OnInit, Output} from '@angular/core';
import {BrokerageStockOrder, PendingStockPosition, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {stockPendingPositionExportLink} from "../../services/links.service";
import {BrokerageService} from "../../services/brokerage.service";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";
import {CurrencyPipe, DatePipe, PercentPipe} from "@angular/common";
import {BrokerageOrdersComponent} from "../../brokerage/brokerage-orders.component";
import {FormsModule} from "@angular/forms";

@Component({
    selector: 'app-stock-trading-pendingpositions',
    templateUrl: './stock-trading-pendingpositions.component.html',
    styleUrls: ['./stock-trading-pendingpositions.component.css'],
    imports: [
        ErrorDisplayComponent,
        LoadingComponent,
        StockLinkAndTradingviewLinkComponent,
        PercentPipe,
        CurrencyPipe,
        DatePipe,
        BrokerageOrdersComponent,
        FormsModule
    ],
    standalone: true
})
export class StockTradingPendingPositionsComponent implements OnInit {
    errors: string[];
    orders: BrokerageStockOrder[];
    positions: PendingStockPosition[] = [];
    loading = {
        positions: true,
        orders: true
    }
    @Output()
    pendingPositionClosed: EventEmitter<PendingStockPosition> = new EventEmitter<PendingStockPosition>()

    constructor(
        private stockService: StocksService,
        private brokerage: BrokerageService
    ) {
    }

    ngOnInit(): void {
        this.refreshPendingPositions()
    }

    refreshPendingPositions() {
        this.stockService.getPendingStockPositions().subscribe(
            (data) => {
                this.positions = data;
                this.loading.positions = false;
            }, err => {
                console.log(err)
                this.errors = GetErrors(err);
                this.loading.positions = false;
            }
        )

        this.brokerage.brokerageAccount().subscribe(
            (data) => {
                this.orders = data.stockOrders;
                this.loading.orders = false;
            }, err => {
                console.log(err)
                this.errors = GetErrors(err);
                this.loading.orders = false;
            }
        )
    }

    closingPosition:PendingStockPosition = null
    closeReason: string = null
    closeErrors: string[] = null
    showCloseModel(position: PendingStockPosition) {
        this.closingPosition = position
    }
    closeCloseModal() {
        this.closingPosition = null
    }
    confirmClosePosition(button: HTMLButtonElement) {
        button.disabled = true
        this.stockService.closePendingPosition(this.closingPosition.id, this.closeReason).subscribe(
            (_) => {
                this.pendingPositionClosed.emit(this.closingPosition);
                this.closingPosition = null;
                this.closeReason = null;
                button.disabled = false
                this.refreshPendingPositions()
            },
            (error) => {
                this.closeErrors = GetErrors(error)
                button.disabled = false
                console.log(error)
            }
        )
    }

    getPendingPositionExportUrl() {
        return stockPendingPositionExportLink()
    }

    protected readonly close = close;
}

