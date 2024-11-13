import {Component, OnInit} from '@angular/core';
import {CurrencyPipe, DecimalPipe, NgClass, NgForOf, NgIf, PercentPipe} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {OptionChain, OptionDefinition, StocksService} from "../../services/stocks.service";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {GetErrors} from "../../services/utils";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";
import {StockSearchComponent} from "../../stocks/stock-search/stock-search.component";

interface OptionLeg {
    option: OptionDefinition;
    action: 'buy' | 'sell';
    quantity: number;
}

interface SpreadCandidate {
    longOption:OptionDefinition,
    shortOption:OptionDefinition,
    cost:number
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
        StockLinkAndTradingviewLinkComponent,
        RouterLink,
        StockSearchComponent
    ],
  templateUrl: './option-spread-builder.component.html',
  styleUrl: './option-spread-builder.component.css'
})
export class OptionSpreadBuilderComponent implements OnInit {
    
    constructor(private stockService: StocksService, private route: ActivatedRoute) {
    }
    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            const ticker = params.get('ticker');
            if (ticker) {
                this.initTicker(ticker);
            }
        }, error => {
            this.errors = GetErrors(error);
        })
    }
    
    initTicker(value:string) {
        this.ticker = value;
        this.loadOptions(value);
    }

    // option business
    ticker: string;
    options: OptionDefinition[] = []; // Will be populated with stub data
    selectedLegs: OptionLeg[] = [];
    filteredOptions: OptionDefinition[] = [];
    uniqueExpirations: string[];

    // filtering business
    sortColumn: string = 'strike';
    sortDirection: 'asc' | 'desc' = 'asc';
    filterExpiration: string = '';
    filterType: 'all' | 'call' | 'put' = 'all';
    filterVolumeOI: 'all' | 'notzero' = 'notzero';
    filterBid: 'all' | 'notzero' = 'all';
    filterMinimumStrike: number = 0;
    
    manualSelection: boolean = false;
    
    optionChain: OptionChain;

    errors: string[] = [];
    loading: boolean = false;

    putOpenInterest() {
        return this.filteredOptions.filter(x => x.optionType == "Put").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    callOpenInterest() {
        return this.filteredOptions.filter(x => x.optionType == "Call").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    putVolume() {
        return this.filteredOptions.filter(x => x.optionType == "Put").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    callVolume() {
        return this.filteredOptions.filter(x => x.optionType == "Call").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    loadOptions(ticker:string): void {
        this.loading = true
        this.stockService.getOptionChain(ticker).subscribe(
            (data) => {
                this.optionChain = data
                this.options = this.optionChain.options
                this.createOptionBasedFilters();
                this.loadFiltersFromLocalStorage()
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
    
    createOptionBasedFilters(): void {
        this.uniqueExpirations = this.options.reduce((expirations, option) => {
            if (!expirations.includes(option.expirationDate)) {
                expirations.push(option.expirationDate);
            }
            return expirations;
        }, []);
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
        
        this.storeFiltersInLocalStorage();
        
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
        
        console.log("filteredOptions", this.filteredOptions)
    }
    
    storeFiltersInLocalStorage() {
        // store the filters in local storage by ticker
        const key = `optionSpreadBuilderFilters-${this.ticker}`;
        const filters = {
            filterExpiration: this.filterExpiration,
            filterType: this.filterType,
            filterVolumeOI: this.filterVolumeOI,
            filterBid: this.filterBid,
            filterMinimumStrike: this.filterMinimumStrike
        }
        
        // store the filters in local storage
        localStorage.setItem(key, JSON.stringify(filters));
    }
    
    loadFiltersFromLocalStorage() {
        const key = `optionSpreadBuilderFilters-${this.ticker}`;
        const filters = JSON.parse(localStorage.getItem(key));
        if (filters) {
            this.filterExpiration = filters.filterExpiration;
            this.filterType = filters.filterType;
            this.filterVolumeOI = filters.filterVolumeOI;
            this.filterBid = filters.filterBid;
            this.filterMinimumStrike = filters.filterMinimumStrike;
        }
    }
    
    clearFilters(): void {
        this.filterExpiration = '';
        this.filterType = 'all';
        this.filterVolumeOI = 'notzero';
        this.filterBid = 'all';
        this.filterMinimumStrike = 0;
        this.applyFiltersAndSort();
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
    
    isBuyLeg(option: OptionDefinition): boolean {
        return this.selectedLegs.some(x => x.option === option && x.action === 'buy');
    }
    
    isSellLeg(option: OptionDefinition): boolean {
        return this.selectedLegs.some(x => x.option === option && x.action === 'sell');
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
    
    selectForTrade(candidate:SpreadCandidate) {
        this.addBuy(candidate.longOption)
        this.addSell(candidate.shortOption)
    }

    calculateLeastFavorable(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.action === 'buy' ? leg.option.ask : leg.option.bid;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier);
        }, 0);
    }
    calculateMostFavorable(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.action === 'buy' ? leg.option.bid : leg.option.ask;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier);
        }, 0);
    }

    calculateMidOfFavorables(): number {
        return (this.calculateLeastFavorable() + this.calculateMostFavorable()) / 2;
    }

    calculateUsingLast(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.option.last;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier);
        }, 0);
    }
    
    currentSpread() : number {
        const maxStrikePrice = Math.max(...this.selectedLegs.map(x => x.option.strikePrice));
        const minStrikePrice = Math.min(...this.selectedLegs.map(x => x.option.strikePrice));
        return maxStrikePrice - minStrikePrice;
    }
    
    calculateUsingMark(): number {
        return this.selectedLegs.reduce((total, leg) => {
            const price = leg.option.mark;
            const multiplier = leg.action === 'buy' ? -1 : 1;
            return total + (price * leg.quantity * multiplier);
        }, 0);
    }
    
    calculateUsingSpreadPercentage(percentage:number) {
        // can only do this if there are two legs
        if (this.selectedLegs.length !== 2) {
            return 0;
        }

        return this.currentSpread() * percentage;
    }
    
    abs(number: number) {
        return Math.abs(number);
    }
    
    daysToExpiration(expirationAsString: string) : number {
        return Math.floor((Date.parse(expirationAsString) - Date.now()) / (24 * 60 * 60 * 1000));
    }

    clearLegs() {
        this.selectedLegs = [];
    }
    
    netDelta() {
        return this.selectedLegs.reduce((total, leg) => {
            const delta = leg.option.delta;
            const multiplier = leg.action === 'buy' ? 1 : -1;
            return total + (delta * multiplier);
        }, 0);
    }
    
    netGamma() {
        return this.selectedLegs.reduce((total, leg) => {
            const gamma = leg.option.gamma;
            const multiplier = leg.action === 'buy' ? 1 : -1;
            return total + (gamma * multiplier);
        }, 0);
    }
    
    netTheta() {
        return this.selectedLegs.reduce((total, leg) => {
            const theta = leg.option.theta;
            const multiplier = leg.action === 'buy' ? 1 : -1;
            return total + (theta * multiplier);
        }, 0);
    }
    
    netVega() {
        return this.selectedLegs.reduce((total, leg) => {
            const vega = leg.option.vega;
            const multiplier = leg.action === 'buy' ? 1 : -1;
            return total + (vega * multiplier);
        }, 0);
    }
    
    adjustExpiration(expirationIndex: number) {
        // first, get the expiration date that is used for the current selected legs
        if (this.selectedLegs.length === 0) {
            return;
        }
        
        const selectedLegExpiration = this.selectedLegs[0].option.expirationDate
        const currentIndex = this.uniqueExpirations.indexOf(selectedLegExpiration);
        let newIndex = currentIndex + expirationIndex;
        
        if (newIndex < 0) {
            newIndex = this.uniqueExpirations.length - 1; 
        } else if (newIndex >= this.uniqueExpirations.length) {
            newIndex = 0;
        }
        
        // now rebuild the legs by selecting the same strike price and option type
        // but with the new expiration date
        let newLegs = []
        for (let leg of this.selectedLegs) {
            let option = this.options.find(x => x.optionType == leg.option.optionType && x.strikePrice == leg.option.strikePrice && x.expirationDate == this.uniqueExpirations[newIndex])
            if (option) {
                newLegs.push({ option, action: leg.action, quantity: leg.quantity });
            }
        }
        this.selectedLegs = newLegs;
    }
    
    adjustStrikes(strike: string) {
        // this.filterMinimumStrike = strike;
        // this.applyFiltersAndSort();
    }

    adjustWidth(width: string) {
        // this.filterWidth = width;
        // this.applyFiltersAndSort();
    }

    flipPosition() {}
    mirrorStrikes() {}
    rollToExpiration(input) {}

    builtSpreads: SpreadCandidate[] = null
    
    findDebitCallSpreads() {
        let spreads : {longOption:OptionDefinition, shortOption:OptionDefinition, cost:number}[] = []
        
        // first filter out available options to a valid call option
        // the filter should take options that are 30 to 90 days out
        // spread should be 10% or less of the stock price
        // some open interest, and some volume
        let minExpirationDate = Date.now() + 30 * 24 * 60 * 60 * 1000
        let maxExpirationDate = Date.now() + 90 * 24 * 60 * 60 * 1000
        
        let callOptions = this.options.filter(x => 
            x.optionType == "Call" 
            && x.openInterest > 0 
            && x.volume > 0
            && Math.abs(x.bid - x.ask) <= 0.1 * x.mark
            && Date.parse(x.expirationDate) > minExpirationDate
            && Date.parse(x.expirationDate) < maxExpirationDate)
        
        for (let longOption of callOptions) {
            const validLong = 
                longOption.delta > 0.35
                && longOption.delta < 0.8
            if (validLong) {
                for (let shortOption of callOptions) {
                    const validShort = 
                        shortOption.strikePrice > longOption.strikePrice
                        && Date.parse(shortOption.expirationDate) == Date.parse(longOption.expirationDate)
                        && shortOption.delta <= 0.35
                    if (validShort) {
                        let cost = longOption.mark - shortOption.mark
                        spreads.push({ longOption, shortOption, cost })
                    }
                }
            }
        }
        
        // sort spreads, first by spread, then cost
        this.builtSpreads = spreads.sort((a, b) => {
            let aSpread = Math.abs(a.longOption.strikePrice - a.shortOption.strikePrice) - a.cost
            let bSpread = Math.abs(b.longOption.strikePrice - b.shortOption.strikePrice) - a.cost
            
            if (aSpread == bSpread) {
                return a.cost - b.cost
            } else {
                return aSpread - bSpread
            }
        })
    }

    findCreditCallSpreads() {
        let spreads: SpreadCandidate[] = []

        // Filter options 30-90 days out
        // Spread should be 10% or less of stock price
        // Require some open interest and volume
        let minExpirationDate = Date.now() + 30 * 24 * 60 * 60 * 1000
        let maxExpirationDate = Date.now() + 90 * 24 * 60 * 60 * 1000

        let callOptions = this.options.filter(x =>
            x.optionType == "Call"
            && x.openInterest > 0
            && x.volume > 0
            && Math.abs(x.bid - x.ask) <= 0.1 * x.mark
            && Date.parse(x.expirationDate) > minExpirationDate
            && Date.parse(x.expirationDate) < maxExpirationDate)

        // For credit spreads, we sell the lower strike and buy the higher strike
        for (let shortOption of callOptions) {
            const validShort =
                shortOption.delta > 0.35
                && shortOption.delta < 0.65  // More conservative delta range for selling options

            if (validShort) {
                for (let longOption of callOptions) {
                    const validLong =
                        longOption.strikePrice > shortOption.strikePrice  // Higher strike for protection
                        && Date.parse(longOption.expirationDate) == Date.parse(shortOption.expirationDate)
                        && longOption.delta <= 0.35  // Further OTM for protection

                    if (validLong) {
                        // Credit received is the difference between what we receive for selling
                        // the lower strike and what we pay for the higher strike
                        let credit = shortOption.mark - longOption.mark

                        // Only include spreads where we receive a credit
                        if (credit > 0) {
                            spreads.push({ longOption, shortOption, cost: credit })
                        }
                    }
                }
            }
        }

        // Sort spreads by maximum return on risk
        // For credit spreads: (credit received) / (spread width - credit received)
        this.builtSpreads = spreads.sort((a, b) => {
            let aSpreadWidth = Math.abs(a.longOption.strikePrice - a.shortOption.strikePrice)
            let bSpreadWidth = Math.abs(b.longOption.strikePrice - b.shortOption.strikePrice)

            let aRiskRewardRatio = a.cost / (aSpreadWidth - a.cost)
            let bRiskRewardRatio = b.cost / (bSpreadWidth - b.cost)

            // Sort by risk/reward ratio in descending order (higher is better)
            return bRiskRewardRatio - aRiskRewardRatio
        })
    }

    findDebitPutSpreads() {
        let spreads: SpreadCandidate[] = []

        // Filter for put options 30-90 days out
        let minExpirationDate = Date.now() + 30 * 24 * 60 * 60 * 1000
        let maxExpirationDate = Date.now() + 90 * 24 * 60 * 60 * 1000

        let putOptions = this.options.filter(x =>
            x.optionType == "Put"  // Changed to Put
            && x.openInterest > 0
            && x.volume > 0
            && Math.abs(x.bid - x.ask) <= 0.1 * x.mark
            && Date.parse(x.expirationDate) > minExpirationDate
            && Date.parse(x.expirationDate) < maxExpirationDate)

        // For put debit spreads, we buy the higher strike and sell the lower strike
        for (let longOption of putOptions) {
            const validLong =
                longOption.delta < -0.35  // Negative delta for puts
                && longOption.delta > -0.8 // More aggressive delta for long put

            if (validLong) {
                for (let shortOption of putOptions) {
                    const validShort =
                        shortOption.strikePrice < longOption.strikePrice  // Lower strike for short put
                        && Date.parse(shortOption.expirationDate) == Date.parse(longOption.expirationDate)
                        && shortOption.delta >= -0.35  // Less aggressive delta for short put

                    if (validShort) {
                        // Cost is what we pay for higher strike minus what we receive for lower strike
                        let cost = longOption.mark - shortOption.mark

                        // Only include spreads where the cost is positive (debit spread)
                        if (cost > 0) {
                            spreads.push({ longOption, shortOption, cost })
                        }
                    }
                }
            }
        }

        // Sort spreads by potential return relative to cost
        this.builtSpreads = spreads.sort((a, b) => {
            // Maximum profit is the difference in strikes minus the cost
            let aMaxProfit = Math.abs(a.longOption.strikePrice - a.shortOption.strikePrice) - a.cost
            let bMaxProfit = Math.abs(b.longOption.strikePrice - b.shortOption.strikePrice) - b.cost

            // Return on Investment (ROI) = maxProfit / cost
            let aROI = aMaxProfit / a.cost
            let bROI = bMaxProfit / b.cost

            if (aROI == bROI) {
                // If ROI is the same, prefer the spread with lower cost
                return a.cost - b.cost
            } else {
                // Sort by ROI in descending order (higher is better)
                return bROI - aROI
            }
        })
    }

    findCreditPutSpreads() {
        let spreads: SpreadCandidate[] = []

        // Filter for put options 30-90 days out
        let minExpirationDate = Date.now() + 30 * 24 * 60 * 60 * 1000
        let maxExpirationDate = Date.now() + 90 * 24 * 60 * 60 * 1000

        let putOptions = this.options.filter(x =>
            x.optionType == "Put"
            && x.openInterest > 0
            && x.volume > 0
            && Math.abs(x.bid - x.ask) <= 0.1 * x.mark
            && Date.parse(x.expirationDate) > minExpirationDate
            && Date.parse(x.expirationDate) < maxExpirationDate)

        // For put credit spreads, we sell the higher strike and buy the lower strike
        for (let shortOption of putOptions) {
            const validShort =
                shortOption.delta > -0.65  // Less aggressive delta for short put
                && shortOption.delta < -0.35  // Not too far OTM

            if (validShort) {
                for (let longOption of putOptions) {
                    const validLong =
                        longOption.strikePrice < shortOption.strikePrice  // Lower strike for protection
                        && Date.parse(longOption.expirationDate) == Date.parse(shortOption.expirationDate)
                        && longOption.delta >= -0.35  // Further OTM for cheaper protection

                    if (validLong) {
                        // Credit received is what we get for selling higher strike minus what we pay for lower strike
                        let credit = shortOption.mark - longOption.mark

                        // Only include spreads where we receive a credit
                        if (credit > 0) {
                            spreads.push({ longOption, shortOption, cost: credit })
                        }
                    }
                }
            }
        }

        // Sort spreads by return on risk
        this.builtSpreads = spreads.sort((a, b) => {
            let aSpreadWidth = Math.abs(a.shortOption.strikePrice - a.longOption.strikePrice)
            let bSpreadWidth = Math.abs(b.shortOption.strikePrice - b.longOption.strikePrice)

            // Return on Risk = credit / (spread width - credit)
            let aReturnOnRisk = a.cost / (aSpreadWidth - a.cost)
            let bReturnOnRisk = b.cost / (bSpreadWidth - b.cost)

            if (aReturnOnRisk === bReturnOnRisk) {
                // If return on risk is equal, prefer the trade with higher credit
                return b.cost - a.cost
            } else {
                // Sort by return on risk in descending order (higher is better)
                return bReturnOnRisk - aReturnOnRisk
            }
        })
    }
}
