import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { BrokerageAccount, OutcomeValueTypeEnum, StockPosition, StockQuote} from '../../services/stocks.service';
import {CurrencyPipe, DecimalPipe, NgClass, PercentPipe} from "@angular/common";
import {toggleVisuallyHidden} from "../../services/utils";
import { StockTradingPositionComponent } from "./stock-trading-position.component";
import { StockLinkComponent } from "src/app/shared/stocks/stock-link.component";
import { StockLinkAndTradingviewLinkComponent } from "src/app/shared/stocks/stock-link-and-tradingview-link.component";


@Component({
    selector: 'app-stock-trading-positions',
    templateUrl: './stock-trading-positions.component.html',
    styleUrls: ['./stock-trading-positions.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe],
    imports: [StockTradingPositionComponent, CurrencyPipe, PercentPipe, StockLinkAndTradingviewLinkComponent, NgClass]
})
export class StockTradingPositionsComponent {
    private percentPipe = inject(PercentPipe);
    private currencyPipe = inject(CurrencyPipe);
    private decimalPipe = inject(DecimalPipe);


    @Input()
    positions: StockPosition[] = [];
    @Input()
    brokerageAccount: BrokerageAccount | null = null;
    @Output()
    positionChanged = new EventEmitter()

    private _quotes: Map<string, StockQuote> = new Map<string, StockQuote>()
    private _visibleDetails: Set<string> = new Set<string>();
    sortColumn: string = '';
    sortDirection: 'asc' | 'desc' = 'desc';

    get quotes() {
        return this._quotes
    }

    @Input()
    set quotes(val: Map<string, StockQuote>) {
        this._quotes = val
    }

    toggleVisibility(elem: HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    getQuote(p: StockPosition) {
        if (this.quotes) {
            return this.quotes[p.ticker]
        }
        return null
    }

    getPrice(p: StockPosition) {
        return this.getQuote(p)?.price || 0
    }

    getOrdersForPosition(ticker: string) {
        if (!this.brokerageAccount) {
            return [];
        }
        return this.brokerageAccount.stockOrders.filter(o => o.ticker === ticker);
    }

    toggleDetails(ticker: string) {
        if (this._visibleDetails.has(ticker)) {
            this._visibleDetails.delete(ticker);
        } else {
            this._visibleDetails.add(ticker);
        }
    }

    isDetailsVisible(ticker: string): boolean {
        return this._visibleDetails.has(ticker);
    }

    sortBy(column: string) {
        if (this.sortColumn === column) {
            // Toggle direction if clicking same column
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            // New column, default to descending
            this.sortColumn = column;
            this.sortDirection = 'desc';
        }

        this.positions = [...this.positions].sort((a, b) => {
            let aValue: any;
            let bValue: any;

            switch (column) {
                case 'ticker':
                    aValue = a.ticker;
                    bValue = b.ticker;
                    break;
                case 'price':
                    aValue = this.getPrice(a);
                    bValue = this.getPrice(b);
                    break;
                case 'avgCost':
                    aValue = a.averageCostPerShare;
                    bValue = b.averageCostPerShare;
                    break;
                case 'gainLoss':
                    aValue = a.gainPct;
                    bValue = b.gainPct;
                    break;
                case 'cost':
                    aValue = a.cost;
                    bValue = b.cost;
                    break;
                default:
                    return 0;
            }

            if (typeof aValue === 'string' && typeof bValue === 'string') {
                const comparison = aValue.localeCompare(bValue);
                return this.sortDirection === 'asc' ? comparison : -comparison;
            } else {
                const comparison = (aValue || 0) - (bValue || 0);
                return this.sortDirection === 'asc' ? comparison : -comparison;
            }
        });
    }

    getSortIcon(column: string): string {
        if (this.sortColumn !== column) {
            return 'bi-chevron-expand';
        }
        return this.sortDirection === 'asc' ? 'bi-chevron-up' : 'bi-chevron-down';
    }
}

