import {Component, OnInit} from '@angular/core';
import {BrokerageOrder} from "../../services/stocks.service";
import {BrokerageService} from "../../services/brokerage.service";
import {GetErrors} from "../../services/utils";
import {StockTradingNewPositionComponent} from "../stock-trading/stock-trading-new-position.component";
import {StockTradingPendingPositionsComponent} from "../stock-trading/stock-trading-pendingpositions.component";
import {BrokerageOrdersComponent} from "../../brokerage/brokerage-orders.component";

@Component({
    selector: 'app-stock-trading-pending-positions-dashboard',
    templateUrl: './stock-trading-pending-positions-dashboard.component.html',
    standalone: true,
    styleUrls: ['./stock-trading-pending-positions-dashboard.component.css'],
    imports: [
        StockTradingNewPositionComponent,
        StockTradingPendingPositionsComponent,
        BrokerageOrdersComponent
    ]
})
export class StockTradingPendingPositionsDashboardComponent implements OnInit {
    feedbackMessage: string;
    orders: BrokerageOrder[];

    constructor(private brokerage: BrokerageService) {
    }

    ngOnInit(): void {
        this.getOrders()
    }

    brokerageOrderEntered() {
        this.feedbackMessage = "Brokerage order entered";
    }

    getOrders() {
        this.brokerage.brokerageAccount().subscribe(account => {
            this.orders = account.orders
        }, error => {
            this.feedbackMessage = GetErrors(error)[0]
        })
    }

    positionOpened() {
        this.feedbackMessage = "Position opened";
    }

    pendingPositionCreated() {
        this.feedbackMessage = "Pending position created";
    }

    pendingPositionClosed() {
        this.feedbackMessage = "Pending position closed";
    }
}
