import {Component, OnInit} from '@angular/core';
import {stockLists_getAnalysisLink, stockLists_getExportLink} from 'src/app/services/links.service';
import {StockList, StocksService} from 'src/app/services/stocks.service';
import {toggleVisuallyHidden} from 'src/app/services/utils';

@Component({
    selector: 'app-stock-lists-dashboard',
    templateUrl: './stock-lists-dashboard.component.html',
    styleUrls: ['./stock-lists-dashboard.component.css']
})
export class StockListsDashboardComponent implements OnInit {
    newName: string
    newDescription: string
    lists: StockList[]
    filteredLists: StockList[] = []

    constructor(
        private stockService: StocksService
    ) {
    }

    ngOnInit(): void {
        this.loadLists();
    }

    createList() {
        var obj = {
            name: this.newName,
            description: this.newDescription
        }

        this.stockService.createStockList(obj).subscribe(
            _ => {
                this.newName = null
                this.newDescription = null
                this.loadLists()
            },
            e => {
                console.error(e)
            }
        )
    }

    deleteList(list: StockList) {
        if (confirm(`Are you sure you want to delete ${list.name}?`)) {
            this.stockService.deleteStockList(list.id).subscribe(
                _ => {
                    this.loadLists()
                },
                e => {
                    console.error(e)
                }
            )
        }
    }

    filterListByTicker(ticker: string) {
        var filteredList = this.lists.filter(
            l => l.tickers.some(
                t => t.ticker.toLowerCase() === ticker.toLowerCase() || ticker === ''
            )
        ).sort((a, b) => a.name.localeCompare(b.name))
        this.filteredLists = filteredList
    }

    getAnalysisLink(list: StockList) {
        return stockLists_getAnalysisLink(list)
    }

    getExportLink(list: StockList) {
        return stockLists_getExportLink(list)
    }

    toggleVisibility(elem) {
        toggleVisuallyHidden(elem)
    }

    private loadLists() {
        this.stockService.getStockLists().subscribe(
            s => {
                this.lists = s;
                this.filteredLists = s;
            },
            e => {
                console.error(e);
            }
        );
    }

}
