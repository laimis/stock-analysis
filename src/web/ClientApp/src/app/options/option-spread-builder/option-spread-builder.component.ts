import {Component, OnInit} from '@angular/core';
import {CurrencyPipe, DecimalPipe, NgClass, NgForOf, NgIf, PercentPipe} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {OptionChain, OptionDefinition, StocksService} from "../../services/stocks.service";
import {ActivatedRoute} from "@angular/router";
import {GetErrors} from "../../services/utils";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";

interface OptionLeg {
    option: OptionDefinition;
    action: 'buy' | 'sell';
    quantity: number;
}


@Component({
  selector: 'app-option-spread-builder',
  standalone: true,
    imports: [
        CurrencyPipe,
        DecimalPipe,
        FormsModule,
        NgForOf,
        NgIf,
        PercentPipe,
        LoadingComponent,
        ErrorDisplayComponent,
        NgClass,
        StockLinkAndTradingviewLinkComponent
    ],
  templateUrl: './option-spread-builder.component.html',
  styleUrl: './option-spread-builder.component.css'
})
export class OptionSpreadBuilderComponent implements OnInit {
    
    constructor(private stockService: StocksService, private route: ActivatedRoute) {
    }
    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            this.ticker = params.get('ticker');
            if (this.ticker) {
                this.loadOptions(this.ticker);
            } else {
                this.errors = ['No ticker provided'];
            }
        }, error => {
            this.errors = GetErrors(error);
        })
    }


    // option business
    ticker: string = 'AAPL'; // Example ticker
    options: OptionDefinition[] = []; // Will be populated with stub data
    selectedLegs: OptionLeg[] = [];
    filteredOptions: OptionDefinition[] = [];
    
    sortColumn: string = 'strike';
    sortDirection: 'asc' | 'desc' = 'asc';
    filterExpiration: string = '';
    filterType: 'all' | 'call' | 'put' = 'all';
    filterVolumeOI: 'all' | 'notzero' = 'notzero';
    filterBid: 'all' | 'notzero' = 'all';
    filterMinimumStrike: number = 0;
    optionChain: OptionChain;

    errors: string[] = [];
    loading: boolean = false;

    putOpenInterest() {
        return this.filteredOptions.filter(x => x.optionType == "put").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    callOpenInterest() {
        return this.filteredOptions.filter(x => x.optionType == "call").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    putVolume() {
        return this.filteredOptions.filter(x => x.optionType == "put").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    callVolume() {
        return this.filteredOptions.filter(x => x.optionType == "call").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    loadOptions(ticker:string): void {
        this.loading = true
        this.stockService.getOptionChain(ticker).subscribe(
            (data) => {
                this.optionChain = data
                this.options = this.optionChain.options
                this.applyFiltersAndSort();
                this.refreshLegsIfNeeded();
                this.loading = false
            },
            (error) => {
                this.errors = GetErrors(error);
                this.loading = false
            }
        )
    }

    refreshLegsIfNeeded(): void {
        // see if we have legs added, if so, refresh them
        // to refresh, create a new leg array and load the options from the
        // option chain
        if (this.selectedLegs.length > 0) {
            let newLegs = []
            for (let leg of this.selectedLegs) {
                let option = this.options.find(x => x.optionType == leg.option.optionType && x.strikePrice == leg.option.strikePrice && x.expirationDate == leg.option.expirationDate)
                if (option) {
                    newLegs.push({ option, action: leg.action, quantity: leg.quantity });
                }
            }
            this.selectedLegs = newLegs;
        }
    }

    applyFiltersAndSort(): void {
        this.filteredOptions = this.options.filter(option => {
            return (this.filterExpiration === '' || option.expirationDate === this.filterExpiration) &&
                (this.filterType === 'all' || option.side === this.filterType) &&
                (this.filterVolumeOI === 'all' || (option.volume > 0 || option.openInterest > 0)) &&
                (this.filterBid === 'all' || option.bid > 0) &&
                option.strikePrice >= this.filterMinimumStrike;
        });

        this.filteredOptions.sort((a, b) => {
            const aValue = a[this.sortColumn];
            const bValue = b[this.sortColumn];
            return this.sortDirection === 'asc' ? aValue - bValue : bValue - aValue;
        });
    }

    setSort(column: string): void {
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = 'asc';
        }
        this.applyFiltersAndSort();
    }

    getUniqueExpirations(): string[] {
        return this.options.reduce((expirations, option) => {
            if (!expirations.includes(option.expirationDate)) {
                expirations.push(option.expirationDate);
            }
            return expirations;
        }, []);
    }

    addBuy(option: OptionDefinition): void {
        this.selectedLegs.push({ option, action: 'buy', quantity: 1 });
    }
    
    addSell(option: OptionDefinition): void {
        this.selectedLegs.push({ option, action: 'sell', quantity: 1 });
    }

    updateLegQuantity(index: number, quantity: number): void {
        this.selectedLegs[index].quantity = quantity;
    }

    removeLeg(index: number): void {
        this.selectedLegs.splice(index, 1);
    }

    calculateLeastFavorable(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.action === 'buy' ? leg.option.ask : leg.option.bid;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier); // Multiply by 100 for contract size
        }, 0);
    }
    calculateMostFavorable(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.action === 'buy' ? leg.option.bid : leg.option.ask;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier); // Multiply by 100 for contract size
        }, 0);
    }

    calculateMidOfFavorables(): number {
        return (this.calculateLeastFavorable() + this.calculateMostFavorable()) / 2;
    }

    calculateUsingLast(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.option.last;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier); // Multiply by 100 for contract size
        }, 0);
    }
    
    calculateUsingMark(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.option.mark;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier); // Multiply by 100 for contract size
        }, 0);
    }
    
    calculateUsingSpreadPercentage(percentage:number) {
        // can only do this if there are two legs
        if (this.selectedLegs.length !== 2) {
            return 0;
        }
        
        const leg1 = this.selectedLegs[0];
        const leg2 = this.selectedLegs[1];
        
        const price1 = leg1.option.strikePrice
        const price2 = leg2.option.strikePrice
        
        return Math.abs(price1 - price2) * percentage;
    }
    
    abs(number: number) {
        return Math.abs(number);
    }
}
