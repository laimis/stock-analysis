import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Router} from '@angular/router';
import {toggleVisuallyHidden} from "../../services/utils";
import {OptionContract, OptionPosition} from "../../services/option.service";

@Component({
    selector: 'app-option-open',
    templateUrl: './option-open.component.html',
    styleUrls: ['./option-open.component.css'],
    standalone: false
})

export class OptionOpenComponent {

    protected readonly toggleVisuallyHidden = toggleVisuallyHidden;
    
    cost : number = 0;
    currentCost : number = 0;
    
    @Output() positionsChanged = new EventEmitter();
    @Output() errorOccurred = new EventEmitter<string[]>();
    
    constructor(private router: Router) {
    }

    private _openOptions: OptionPosition[] = []

    get openOptions(): OptionPosition[] {
        return this._openOptions
    }

    @Input()
    set openOptions(value: OptionPosition[]) {
        if (value == null) {
            value = []
        }
        this._openOptions = 
            value.sort((a, b) => b.profit - a.profit)
        
        this.cost = 
            this.openOptions
                .map(op => op.cost)
                .reduce((acc, cost) => acc + cost, 0)
        
        this.currentCost =
            this.openOptions
                .map(op => op.contracts)
                .flat()
                .reduce((acc, contract) => acc + contract.quantity * contract.details?.mark, 0)
    }

    onTickerSelected(ticker: string) {
        this.router.navigateByUrl('/options/chain/' + ticker)
            .then(fullfilled => {
                if (!fullfilled) {
                    console.error('Navigation to /options/chain/' + ticker + ' failed')
                }
            })
    }

    intrinsicValue(option: OptionContract): number {
        if (option.optionType == 'CALL') {
            if (option.details.currentPrice > option.strikePrice) {
                return option.details.currentPrice - option.strikePrice
            } else {
                return 0
            }
        }

        if (option.optionType == 'PUT') {
            if (option.details.currentPrice < option.strikePrice) {
                return option.strikePrice - option.details.currentPrice
            } else {
                return 0
            }
        }

        console.log(option.optionType)
        return 0
    }

    positionCurrentCost(option: OptionPosition) {
        return option.contracts
            .map(contract => contract.quantity * contract.details.mark)
            .reduce((acc, cost) => acc + cost, 0)
    }

    protected readonly open = open;
}
