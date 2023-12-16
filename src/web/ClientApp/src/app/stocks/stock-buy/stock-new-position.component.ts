import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-stock-new-position',
  templateUrl: './stock-new-position.component.html',
  styleUrls: ['./stock-new-position.component.css']
})
export class StockNewPositionComponent {
  feedbackMessage: string;

  brokerageOrderEntered() {
    this.feedbackMessage = "Brokerage order entered";
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
