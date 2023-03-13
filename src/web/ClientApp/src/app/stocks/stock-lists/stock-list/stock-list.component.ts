import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { stockLists_getAnalysisLink, stockLists_getExportLink } from 'src/app/services/links.service';
import { Monitor, StockList, StockListTicker, StocksService } from 'src/app/services/stocks.service';
import { toggleVisuallyHidden } from 'src/app/services/utils';

@Component({
  selector: 'app-stock-list',
  templateUrl: './stock-list.component.html',
  styleUrls: ['./stock-list.component.css']
})
export class StockListComponent implements OnInit {
  list: StockList;
  monitors: Monitor[];
  analysisLink: string;
  exportLink: string;
  exportLinkJustTickers: string;
  importStatus: string

  constructor(
    private stockService: StocksService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    // should read name from the route and then use stock service injected in the constructor to load the list with that name
    
    var name = this.route.snapshot.paramMap.get('name');

    this.loadList(name);
    this.loadMonitors()
  }

  private loadList(name: string) {
    this.stockService.getStockList(name).subscribe(list => {
      this.list = list;
      this.analysisLink = this.getAnalysisLink(list)
      this.exportLink = this.getExportLink(list, false)
      this.exportLinkJustTickers = this.getExportLink(list, true)
    }, e => {
      console.error(e);
    });
  }

  private loadMonitors() {
    this.stockService.getAvailableMonitors().subscribe(monitors => {
      this.monitors = monitors;
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
    var separator = '\n';
    if (tickers.includes(',')) {
      separator = ',';
    }
    
    let tickerArray = tickers.split(separator)

    this.AddTickersToList(tickerArray)
  }

  private AddTickersToList(tickerArray: string[]) {
    if (tickerArray.length == 0) {
      this.loadList(this.list.name)
      this.importStatus = "Finished"
      return
    }
    
    var ticker = tickerArray[0].trim();
    if (ticker.includes(':'))
    {
      ticker = ticker.split(':')[1].trim();
    }
    
    if (ticker) {
      this.importStatus = `Importing ${ticker}...`
      this.stockService.addToStockList(this.list.name, ticker).subscribe(
        _ => {
          this.AddTickersToList(tickerArray.slice(1))
        },
        e => {
          console.error(e);
          this.AddTickersToList(tickerArray.slice(1))
        }
      );
    } else {
      this.AddTickersToList(tickerArray.slice(1))
    }
  }

  sortedTickers(list: StockList) {
    return list.tickers.sort((a, b) => a.ticker.localeCompare(b.ticker));
  }

  getExportLink(list: StockList, justTickers:boolean) {
    return stockLists_getExportLink(list, justTickers)
  }

  toggleVisibility(elem: HTMLElement) {
    toggleVisuallyHidden(elem)
  }
  
  getAnalysisLink(list: StockList) {
    return stockLists_getAnalysisLink(list)
  }
  
  assignTag(tag:string) {
    this.stockService.assignTagToStockList(this.list.name, tag).subscribe(_ => {
      this.loadList(this.list.name)
    }, e => {
      console.error(e);
    });
  }

  containsTag(tag:string) {
    return this.list.tags.includes(tag)
  }

  removeTag(tag:string) {
    this.stockService.removeTagFromStockList(this.list.name, tag).subscribe(_ => {
      this.loadList(this.list.name)
    }, e => {
      console.error(e);
    });
  }
}
