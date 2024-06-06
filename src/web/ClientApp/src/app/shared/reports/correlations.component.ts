import {Component, Input} from '@angular/core';
import {TickerCorrelation} from '../../services/stocks.service';
import {DecimalPipe, NgClass, NgIf} from "@angular/common";
import {LoadingComponent} from "../loading/loading.component";

@Component({
    selector: 'app-correlations',
    templateUrl: './correlations.component.html',
    styleUrls: ['./correlations.component.css'],
    imports: [
        DecimalPipe,
        LoadingComponent,
        NgIf,
        NgClass,
        
    ],
    standalone: true
})
export class CorrelationsComponent {
    private _correlations: TickerCorrelation[];
    correlationTickers: string[];
    sortedCorrelations: TickerCorrelation[];

    @Input()
    set correlations(value: TickerCorrelation[]) {
        this._correlations = value;
        this.correlationTickers = value.map(c => c.ticker)
        this.sortByCorrelation(this.correlationTickers[0])
    }
    get correlations() {
        return this._correlations
    }

    sortByCorrelation(ticker: string) {

        let correlationsToSortBy = this.correlations.find(c => c.ticker === ticker)

        // copy the array so we don't sort the original
        this.sortedCorrelations = this.correlations.map(c => c)
            .sort((a, b) => {
                let aIndex = this.correlations.findIndex(c => c.ticker === a.ticker)
                let bIndex = this.correlations.findIndex(c => c.ticker === b.ticker)

                let aCorrelation = correlationsToSortBy.correlations[aIndex]
                let bCorrelation = correlationsToSortBy.correlations[bIndex]
                return bCorrelation - aCorrelation
            })
    }
    
}
