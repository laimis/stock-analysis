import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-app-stock-new-position',
  templateUrl: './app-stock-new-position.component.html',
  styleUrls: ['./app-stock-new-position.component.css']
})
export class StockNewPositionComponent implements OnInit {
  feedbackMessage: string;

  constructor() { }

  ngOnInit(): void {
  }

  brokerageOrderEntered() {
    this.feedbackMessage = "Brokerage order entered";
  }

  stockPurchased() {
    this.feedbackMessage = "Position open recorded";
  }

}
