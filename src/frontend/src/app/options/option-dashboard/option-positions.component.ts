import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Router} from '@angular/router';
import {toggleVisuallyHidden} from "../../services/utils";
import {BrokerageOptionOrder, OptionContract, OptionPosition} from "../../services/option.service";
import {OptionPositionComponent} from "../option-position/option-position.component";
import { CurrencyPipe, NgClass } from "@angular/common";
import {StockSearchComponent} from "../../stocks/stock-search/stock-search.component";
import { StockLinkComponent } from "../../shared/stocks/stock-link.component";

@Component({
    selector: 'app-option-positions',
    templateUrl: './option-positions.component.html',
    imports: [
    OptionPositionComponent,
    CurrencyPipe,
    StockSearchComponent,
    StockLinkComponent
],
    styleUrls: ['./option-positions.component.css']
})

export class OptionPositionsComponent {

    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;
    
    cost : number = 0;
    currentCost : number = 0;
    
    @Output() positionsChanged = new EventEmitter();
    @Output() errorOccurred = new EventEmitter<string[]>();
    
    constructor(private router: Router) {
    }

    private _positions: OptionPosition[] = []

    @Input()
    set positions(value: OptionPosition[]) {
        if (value == null) {
            value = []
        }
        this._positions = 
            value.sort((a, b) => b.profit - a.profit)
        
        this.cost = 
            this.positions
                .map(op => op.cost)
                .reduce((acc, cost) => acc + cost, 0)
        
        this.currentCost =
            this.positions
                .map(op => op.contracts)
                .flat()
                .reduce((acc, contract) => acc + contract.quantity * (contract.details?.mark || 0), 0)
    }
    get positions(): OptionPosition[] {
        return this._positions
    }
    
    @Input()
    orders : BrokerageOptionOrder[]

    onTickerSelected(ticker: string) {
        this.router.navigateByUrl('/options/chain/' + ticker)
            .then(fullfilled => {
                if (!fullfilled) {
                    console.error('Navigation to /options/chain/' + ticker + ' failed')
                }
            })
    }

    intrinsicValue(option: OptionContract): number {
        if (option.details == null) {
            return 0
        }

        if (option.optionType == 'CALL') {
            if (option.details.underlyingPrice > option.strikePrice) {
                return option.details.underlyingPrice - option.strikePrice
            } else {
                return 0
            }
        }

        if (option.optionType == 'PUT') {
            if (option.details.underlyingPrice < option.strikePrice) {
                return option.strikePrice - option.details.underlyingPrice
            } else {
                return 0
            }
        }

        return 0
    }

    positionCurrentCost(option: OptionPosition) {
        return option.contracts
            .map(contract => contract.quantity * (contract.details?.mark || 0))
            .reduce((acc, cost) => acc + cost, 0)
    }

    protected readonly open = open;

    expandedPositions: boolean[] = [];
    toggleExpandedPositions(index: number) {
        // Initialize array if needed
        if (!this.expandedPositions) {
            this.expandedPositions = new Array(this.positions.length).fill(false);
        }
        // Toggle the expanded state
        this.expandedPositions[index] = !this.expandedPositions[index];
    }
}
