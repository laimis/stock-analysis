import {Component, Input} from '@angular/core';
import {TradingStrategyResult, TradingStrategyResults} from "../../services/stocks.service";
import { CurrencyPipe, DecimalPipe, NgClass, PercentPipe } from '@angular/common';
import { ParsedDatePipe } from 'src/app/services/parsedDate.filter';


@Component({
    selector: 'app-trading-actual-vs-simulated',
    templateUrl: './trading-actual-vs-simulated.component.html',
    styleUrls: ['./trading-actual-vs-simulated.component.css'],
    imports: [CurrencyPipe, DecimalPipe, ParsedDatePipe, NgClass, PercentPipe, DecimalPipe],
    standalone: true
})
export class TradingActualVsSimulatedPositionComponent {

    showDetails: number | null = null;

    results: TradingStrategyResult[] = [];
    sortedResults: TradingStrategyResult[] = [];
    
    @Input()
    set simulations(value: TradingStrategyResults) {
        this.results = value.results;
        this.sortResults();
    }

    @Input()
    simulationErrors: string[] = []

    sortColumn: string = 'profit';
    sortDirection: string = 'desc';
    
    sortChanged(column: string) {
        if (this.sortColumn == column) {
            this.sortDirection = this.sortDirection == 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = 'desc';
        }
        this.sortResults()
    }
    
    toggleShowDetails(index: number) {
        if (this.showDetails == index) {
            this.showDetails = null;
        } else {
            this.showDetails = index;
        }
    }

    sortResults() {
        console.log('sortResults', this.sortColumn, this.sortDirection);
        this.sortedResults = this.results.sort((a: TradingStrategyResult, b: TradingStrategyResult) => {
            const aRecord = a as unknown as Record<string, Record<string, unknown>>;
            const bRecord = b as unknown as Record<string, Record<string, unknown>>;
            // we either use a and b directory or .position on a and b
            // depending on the sort column
            const bSort = (bRecord.position[this.sortColumn] == null) ? bRecord[this.sortColumn] : bRecord.position[this.sortColumn];
            const aSort = (aRecord.position[this.sortColumn] == null) ? aRecord[this.sortColumn] : aRecord.position[this.sortColumn];
            
            if (this.sortDirection == 'desc') {
                return (bSort as number) - (aSort as number);
            } else {
                return (aSort as number) - (bSort as number);
            }});
        
    }
}

