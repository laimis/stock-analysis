import { Component, OnInit, inject } from '@angular/core';
import {stockLists_getAnalysisLink, stockLists_getExportLink} from 'src/app/services/links.service';
import {StockList, StocksService} from 'src/app/services/stocks.service';
import {GetErrors, toggleVisuallyHidden} from 'src/app/services/utils';

@Component({
    selector: 'app-stock-lists-dashboard',
    templateUrl: './stock-lists-dashboard.component.html',
    styleUrls: ['./stock-lists-dashboard.component.css'],
    standalone: false
})
export class StockListsDashboardComponent implements OnInit {
    private stockService = inject(StocksService);

    newName: string
    newDescription: string
    lists: StockList[]
    filteredLists: StockList[] = []
    errors: string[] = []

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
                this.errors = GetErrors(e)
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
                    this.errors = GetErrors(e)
                }
            )
        }
    }

    filterListByTicker(ticker: string) {
        this.filteredLists = this.lists.filter(
            l => l.tickers.some(
                t => t.ticker.toLowerCase() === ticker.toLowerCase() || ticker === ''
            )
        ).sort((a, b) => a.name.localeCompare(b.name))
    }

    getAnalysisLink(list: StockList) {
        return stockLists_getAnalysisLink(list)
    }

    getExportLink(list: StockList) {
        return stockLists_getExportLink(list)
    }

    toggleVisibility(elem: HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    private loadLists() {
        this.stockService.getStockLists().subscribe(
            s => {
                this.lists = s;
                this.filteredLists = s;
            },
            e => {
                this.errors = GetErrors(e)
            }
        );
    }

}
