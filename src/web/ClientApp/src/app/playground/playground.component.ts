import { Component, OnInit } from '@angular/core';
import { Prices, StocksService } from '../services/stocks.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-playground',
  templateUrl: './playground.component.html',
  styleUrls: ['./playground.component.css']
})

export class PlaygroundComponent implements OnInit {
  prices: Prices;
  
  constructor(private stocks:StocksService, private route: ActivatedRoute) { }

  ngOnInit() {
    console.log('PlaygroundComponent.ngOnInit()');
    var ticker = this.route.snapshot.queryParamMap.get('ticker');
    console.log('ticker: ' + ticker);
    if (ticker){
      this.stocks.getStockPrices(ticker, 720).subscribe( prices => {
        this.prices = prices
      });
    }
  }
}

