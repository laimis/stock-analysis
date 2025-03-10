import {Component, OnInit} from '@angular/core';
import {BrokerageAccount, BrokerageStockOrder} from "../../services/stocks.service";
import {BrokerageService} from "../../services/brokerage.service";
import {GetErrors} from "../../services/utils";
import {StockTradingNewPositionComponent} from "../stock-trading/stock-trading-new-position.component";
import {StockTradingPendingPositionsComponent} from "../stock-trading/stock-trading-pendingpositions.component";
import {BrokerageOrdersComponent} from "../../brokerage/brokerage-orders.component";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {NgClass, NgIf} from "@angular/common";
import {
    StockTradingPendingpositionAnalysisComponent
} from "../stock-trading/stock-trading-pendingposition-analysis.component";
import {BrokerageNewOrderComponent} from "../../brokerage/brokerage-new-order.component";

@Component({
    selector: 'app-stock-trading-pending-positions-dashboard',
    templateUrl: './stock-trading-pending-positions-dashboard.component.html',
    styleUrls: ['./stock-trading-pending-positions-dashboard.component.css'],
    imports: [
        StockTradingNewPositionComponent,
        StockTradingPendingPositionsComponent,
        BrokerageOrdersComponent,
        RouterLink,
        NgClass,
        NgIf,
        StockTradingPendingpositionAnalysisComponent,
        BrokerageNewOrderComponent
    ]
})
export class StockTradingPendingPositionsDashboardComponent implements OnInit {
    feedbackMessage: string;
    account: BrokerageAccount;
    
    constructor(
        private brokerage: BrokerageService,
        private route: ActivatedRoute) {
    }

    ngOnInit(): void {
        this.route.params.subscribe(param => {
            this.activeTab = param['tab'] || 'positions'
        })
        this.getOrders()
        
    }

    brokerageOrderEntered() {
        this.feedbackMessage = "Brokerage order entered";
    }

    getOrders() {
        this.brokerage.brokerageAccount().subscribe(account => {
            this.account = account
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
    
    activeTab = 'pending';
    isActive(tab: string) {
        return tab === this.activeTab;
    }
    activateTab(tab: string) {
        this.activeTab = tab;
    }
}
