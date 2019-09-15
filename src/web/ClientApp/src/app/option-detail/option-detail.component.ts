import { Component, OnInit } from '@angular/core';
import { StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-option-detail',
  templateUrl: './option-detail.component.html',
  styleUrls: ['./option-detail.component.css']
})
export class OptionDetailComponent implements OnInit {
  ticker: string;

  public option : object

  constructor(
    private service: StocksService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    var ticker = this.route.snapshot.paramMap.get('ticker');
    if (ticker) {
      this.ticker = ticker;
    }

    this.option = {
      ticker: ticker,
      currentSharePrice: 2.17,
      available: [
        {
          "expiration" : "2019-09-20",
          strikes: [
            {
              strikePrice: 2.5,
              openInterest: 10,
              volume: 5,
              bid: 10,
              ask: 15,
              spread: 5
            },
            {
              strikePrice: 3.0,
              openInterest: 10,
              volume: 5,
              bid: 10,
              ask: 15,
              spread: 5
            }
          ]
        }
      ]
    }
  }

}
