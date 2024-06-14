import {Component, EventEmitter, OnInit, Output} from '@angular/core';
import {BrokerageOrder, PendingStockPosition, StocksService} from 'src/app/services/stocks.service';
import {GetErrors} from 'src/app/services/utils';
import {stockPendingPositionExportLink} from "../../services/links.service";
import {BrokerageService} from "../../services/brokerage.service";

@Component({
    selector: 'app-stock-trading-pendingpositions',
    templateUrl: './stock-trading-pendingpositions.component.html',
    styleUrls: ['./stock-trading-pendingpositions.component.css']
})
export class StockTradingPendingPositionsComponent implements OnInit {
    errors: string[];
    orders: BrokerageOrder[];
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
                this.orders = data.orders;
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
    showCloseModel(position: PendingStockPosition) {
        this.closingPosition = position
    }
    closeCloseModal() {
        this.closingPosition = null
    }
    confirmClosePosition() {
        this.stockService.closePendingPosition(this.closingPosition.id, this.closeReason).subscribe(
            (_) => {
                this.pendingPositionClosed.emit(this.closingPosition);
                this.closingPosition = null;
                this.closeReason = null;
                this.refreshPendingPositions()
            },
            (error) => {
                console.log(error)
            }
        )
    }

    getPendingPositionExportUrl() {
        return stockPendingPositionExportLink()
    }
}

