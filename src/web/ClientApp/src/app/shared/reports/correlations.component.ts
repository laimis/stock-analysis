import {Component, Input} from '@angular/core';
import {TickerCorrelation} from '../../services/stocks.service';
import {DecimalPipe, NgClass, NgIf} from "@angular/common";
import {LoadingComponent} from "../loading/loading.component";
import {StockLinkAndTradingviewLinkComponent} from "../stocks/stock-link-and-tradingview-link.component";

@Component({
    selector: 'app-correlations',
    templateUrl: './correlations.component.html',
    styleUrls: ['./correlations.component.css'],
    imports: [
        DecimalPipe,
        LoadingComponent,
        NgIf,
        NgClass,
        StockLinkAndTradingviewLinkComponent
    ],
    standalone: true
})
export class CorrelationsComponent {
    private _correlations: TickerCorrelation[];
    correlationTickers: string[];
    sortedCorrelations: TickerCorrelation[];
    selectedTicker: string;
    sortDirection: number = 1;

    @Input()
    set correlations(value: TickerCorrelation[]) {
        this._correlations = value;
        this.correlationTickers = value.map(c => c.ticker)
        this.sortByCorrelation(this.correlationTickers[0])
    }
    get correlations() {
        return this._correlations
    }
    
    @Input()
    days: number

    sortByCorrelation(ticker: string) {

        if (this.selectedTicker === ticker) {
            this.sortDirection = this.sortDirection * -1
        } else {
            this.sortDirection = 1
        }
        this.selectedTicker = ticker
        
        let correlationsToSortBy = this.correlations.find(c => c.ticker === ticker)

        // copy the array so we don't sort the original
        this.sortedCorrelations = this.correlations.map(c => c)
            .sort((a, b) => {
                let aIndex = this.correlations.findIndex(c => c.ticker === a.ticker)
                let bIndex = this.correlations.findIndex(c => c.ticker === b.ticker)

                let aCorrelation = correlationsToSortBy.correlations[aIndex]
                let bCorrelation = correlationsToSortBy.correlations[bIndex]
                
                if (this.sortDirection === -1) {
                    return aCorrelation - bCorrelation
                }
                
                return bCorrelation - aCorrelation
            })
    }
    
}
