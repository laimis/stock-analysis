import {Component, OnInit} from '@angular/core';
import {BrokerageOrder} from "../../services/stocks.service";
import {BrokerageService} from "../../services/brokerage.service";
import {GetErrors} from "../../services/utils";

@Component({
  selector: 'app-stock-new-position',
  templateUrl: './stock-new-position.component.html',
  styleUrls: ['./stock-new-position.component.css']
})
export class StockNewPositionComponent implements OnInit {
  feedbackMessage: string;
  orders: BrokerageOrder[];

  constructor(private brokerage : BrokerageService) {}

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
