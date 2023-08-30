import { Component, ViewChild } from '@angular/core';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';
import { StockTradingPendingPositionsComponent } from '../stock-trading/stock-trading-pendingpositions.component';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-stock-new-position',
  templateUrl: './stock-new-position.component.html',
  styleUrls: ['./stock-new-position.component.css']
})
export class StockNewPositionComponent {
  feedbackMessage: string;

  constructor(title:Title) {
    title.setTitle("Trade - Nightingale Trading")
  }

  @ViewChild(BrokerageOrdersComponent)
  private brokerageOrders!: BrokerageOrdersComponent;

  @ViewChild(StockTradingPendingPositionsComponent)
  private pendingPositions!: StockTradingPendingPositionsComponent;

  brokerageOrderEntered() {
    this.feedbackMessage = "Brokerage order entered";
    this.brokerageOrders.refreshOrders();
  }

  stockPurchased() {
    this.feedbackMessage = "Position open recorded";
  }

  pendingPositionCreated() {
    this.feedbackMessage = "Pending position created";
    this.pendingPositions.refreshPendingPositions();
    this.brokerageOrders.refreshOrders();
  }

  pendingPositionClosed() {
    this.feedbackMessage = "Pending position closed";
    this.pendingPositions.refreshPendingPositions();
    this.brokerageOrders.refreshOrders();
  }
}
