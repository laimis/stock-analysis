import { Component, ViewChild } from '@angular/core';
import { BrokerageOrdersComponent } from 'src/app/brokerage/orders.component';

@Component({
  selector: 'app-stock-new-position',
  templateUrl: './stock-new-position.component.html',
  styleUrls: ['./stock-new-position.component.css']
})
export class StockNewPositionComponent {
  feedbackMessage: string;

  constructor() { }

  @ViewChild(BrokerageOrdersComponent)
  private brokerageOrders!: BrokerageOrdersComponent;

  brokerageOrderEntered() {
    this.feedbackMessage = "Brokerage order entered";
    this.brokerageOrders.refreshOrders();
  }

  stockPurchased() {
    this.feedbackMessage = "Position open recorded";
  }


}
