import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StockList, StockListTicker, StocksService } from 'src/app/services/stocks.service';

@Component({
  selector: 'app-stock-list',
  templateUrl: './stock-list.component.html',
  styleUrls: ['./stock-list.component.css']
})
export class StockListComponent implements OnInit {
  list: StockList;

  constructor(
    private stockService: StocksService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    // should read name from the route and then use stock service injected in the constructor to load the list with that name
    
    var name = this.route.snapshot.paramMap.get('name');

    this.loadList(name);
  }

  private loadList(name: string) {
    this.stockService.getStockList(name).subscribe(list => {
      this.list = list;
    }, e => {
      console.error(e);
    });
  }

  remove(ticker:StockListTicker) {
    this.stockService.removeFromStockList(this.list.name, ticker.ticker).subscribe(list => {
      this.list = list
    }, e => {
      console.error(e);
    }
    );
  }

  add(tickers: string) {
    tickers.split('\n').forEach(element => {

      var ticker = element.trim();
      if (ticker) {
        this.stockService.addToStockList(this.list.name, ticker).subscribe(_ => {
        }, e => {
          console.error(e);
        });
      }
    });

    this.loadList(this.list.name)
  }

  sortedTickers(list: StockList) {
    return list.tickers.sort((a, b) => a.ticker.localeCompare(b.ticker));
  }
}
