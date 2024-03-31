import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {stockLists_getAnalysisLink, stockLists_getExportLink} from 'src/app/services/links.service';
import {Monitor, StockList, StockListTicker, StocksService} from 'src/app/services/stocks.service';
import {toggleVisuallyHidden} from 'src/app/services/utils';

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
    addInProgress: boolean = false;
    controlToHide: HTMLElement;

    constructor(
        private stockService: StocksService,
        private route: ActivatedRoute
    ) {
    }

    ngOnInit(): void {
        this.route.params.subscribe(params => {
            const name = params['id']
            this.loadList(name);
            this.loadMonitors()
        })
    }

    remove(ticker: StockListTicker) {
        this.stockService.removeFromStockList(this.list.name, ticker.ticker).subscribe(list => {
                this.list = list
            }, e => {
                console.error(e);
            }
        );
    }

    add(tickers: string, controlToHide: HTMLElement) {
        var separator = '\n';
        if (tickers.includes(',')) {
            separator = ',';
        }

        let tickerArray = tickers.split(separator)
        this.controlToHide = controlToHide
        this.addTickersToList(tickerArray)
    }

    sortedTickers(list: StockList) {
        return list.tickers.sort((a, b) => a.ticker.localeCompare(b.ticker));
    }

    getExportLink(list: StockList, justTickers: boolean) {
        return stockLists_getExportLink(list, justTickers)
    }

    toggleVisibility(elem: HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    getAnalysisLink(list: StockList) {
        return stockLists_getAnalysisLink(list)
    }

    clear() {
        // confirm that user wants to clear it

        if (!confirm("Are you sure you want to clear this list?")) {
            return
        }

        this.stockService.clearStockList(this.list.name).subscribe(_ => {
            this.loadList(this.list.name)
        }, e => {
            console.error(e);
        });
    }

    assignTag(tag: string) {
        this.stockService.assignTagToStockList(this.list.name, tag).subscribe(_ => {
            this.loadList(this.list.name)
        }, e => {
            console.error(e);
        });
    }

    containsTag(tag: string) {
        return this.list.tags.includes(tag)
    }

    removeTag(tag: string) {
        this.stockService.removeTagFromStockList(this.list.name, tag).subscribe(_ => {
            this.loadList(this.list.name)
        }, e => {
            console.error(e);
        });
    }

    update(name, description) {
        this.stockService.updateStockList(this.list.name, name, description).subscribe(_ => {
            this.loadList(name)
        }, e => {
            console.error(e);
        });
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

    private addTickersToList(tickerArray: string[]) {
        if (tickerArray.length == 0) {
            this.loadList(this.list.name)
            this.addInProgress = false
            if (this.controlToHide) {
                toggleVisuallyHidden(this.controlToHide)
            }
            return
        }

        this.addInProgress = true

        let ticker = tickerArray[0].trim();
        if (ticker.includes(':')) {
            ticker = ticker.split(':')[1].trim();
        }

        if (ticker) {
            this.stockService.addToStockList(this.list.name, ticker).subscribe(
                _ => {
                    this.addTickersToList(tickerArray.slice(1))
                },
                e => {
                    console.error(e);
                    this.addTickersToList(tickerArray.slice(1))
                }
            );
        } else {
            this.addTickersToList(tickerArray.slice(1))
        }
    }
}
