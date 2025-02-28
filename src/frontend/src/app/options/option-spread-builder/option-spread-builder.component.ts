import {Component, OnInit, Input} from '@angular/core';
import {CurrencyPipe, DecimalPipe, NgClass, NgForOf, NgIf, PercentPipe} from "@angular/common";
import {FormsModule} from "@angular/forms";
import {ActivatedRoute} from "@angular/router";
import {GetErrors} from "../../services/utils";
import {LoadingComponent} from "../../shared/loading/loading.component";
import {ErrorDisplayComponent} from "../../shared/error-display/error-display.component";
import {StockLinkAndTradingviewLinkComponent} from "../../shared/stocks/stock-link-and-tradingview-link.component";
import {StockSearchComponent} from "../../stocks/stock-search/stock-search.component";
import {OptionChain, OptionDefinition, OptionLeg, OptionService} from "../../services/option.service";
import {
    OptionPendingPositionCreateModalComponent
} from "../option-dashboard/option-pendingposition-create-modal/option-pendingposition-create-modal.component";

interface SpreadCandidate {
    longOption:OptionDefinition,
    shortOption:OptionDefinition,
    cost:number
}

enum SpreadType {
    DEBIT_CALL = 'DEBIT_CALL',
    CREDIT_CALL = 'CREDIT_CALL',
    DEBIT_PUT = 'DEBIT_PUT',
    CREDIT_PUT = 'CREDIT_PUT'
  }

@Component({
    selector: 'app-option-spread-builder',
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
        StockSearchComponent,
        OptionPendingPositionCreateModalComponent
    ],
    templateUrl: './option-spread-builder.component.html',
    styleUrl: './option-spread-builder.component.css'
})
export class OptionSpreadBuilderComponent implements OnInit {
    parseInt(input: string): number {
        return Number.parseInt(input);
    }
    
    constructor(private optionService: OptionService, private route: ActivatedRoute) {
    }
    ngOnInit() {
        this.route.paramMap.subscribe(params => {
            this.ticker = params.get('ticker');
        }, error => {
            this.errors = GetErrors(error);
        })
    }

    _ticker: string;
    @Input()
    set ticker(value: string) {
        if (value) {
            this._ticker = value;
            this.loadOptions(value);
        }
    }
    get ticker(): string {
        return this._ticker;
    }
    
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
    filterMaximumSpread: number = 0;
    
    manualSelection: boolean = true;
    
    optionChain: OptionChain;

    errors: string[] = [];
    loading: boolean = false;

    // filter settings for findind spreads
    minSpread = 0;
    maxSpread = 0;
    minCostSpreadRatio = 0;
    maxCostSpreadRatio = 0;
    minExpirationDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]; // 30 days from now
    maxExpirationDate = new Date(Date.now() + 90 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]; // 90 days from now
    maxBidAskSpreadPercent: number = 10; // 10%
    selectedSpreadType: SpreadType = SpreadType.DEBIT_CALL;

    isCallSelected: boolean = true;
    isDebitSelected: boolean = true;

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
        this.optionService.getOptionChain(ticker).subscribe(
            (data) => {
                this.optionChain = data
                this.options = this.optionChain.options
                this.createOptionBasedFilters();
                this.loadFiltersFromLocalStorage();
                this.loadFindSettingsFromLocalStorage();
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
            if (!expirations.includes(option.expiration)) {
                expirations.push(option.expiration);
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
                let option = this.options.find(x => x.optionType == leg.option.optionType && x.strikePrice == leg.option.strikePrice && x.expiration == leg.option.expiration)
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
            return (this.filterExpiration === '' || option.expiration === this.filterExpiration) &&
                (this.filterType === 'all' || option.optionType.toLowerCase() === this.filterType) &&
                (this.filterVolumeOI === 'all' || (option.volume > 0 || option.openInterest > 0)) &&
                (this.filterBid === 'all' || option.bid > 0) &&
                option.strikePrice >= this.filterMinimumStrike &&
                (this.filterMaximumSpread === 0 || option.spread <= this.filterMaximumSpread);
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

    saveFindFiltersAndFindSpreads() {
        const key = `optionSpreadBuilderFindFilters-${this.ticker}`;
        const filters = {
            minSpread: this.minSpread,
            maxSpread: this.maxSpread,
            minCostSpreadRatio: this.minCostSpreadRatio,
            maxCostSpreadRatio: this.maxCostSpreadRatio,
            minExpirationDate: this.minExpirationDate,
            maxExpirationDate: this.maxExpirationDate,
            maxBidAskSpreadPercent: this.maxBidAskSpreadPercent,
            isCallSelected: this.isCallSelected,
            isDebitSelected: this.isDebitSelected
        }
        localStorage.setItem(key, JSON.stringify(filters));
        console.log("saved find filters", filters)

        // Determine the spread type based on the selected options
        let spreadType: SpreadType;
        
        if (this.isCallSelected && this.isDebitSelected) {
            spreadType = SpreadType.DEBIT_CALL;
        } else if (this.isCallSelected && !this.isDebitSelected) {
            spreadType = SpreadType.CREDIT_CALL;
        } else if (!this.isCallSelected && this.isDebitSelected) {
            spreadType = SpreadType.DEBIT_PUT;
        } else {
            spreadType = SpreadType.CREDIT_PUT;
        }

        this.findSpreads(spreadType);
    }

    loadFindSettingsFromLocalStorage() {
        const key = `optionSpreadBuilderFindFilters-${this.ticker}`;
        const filters = JSON.parse(localStorage.getItem(key));
        if (filters) {
            this.minSpread = filters.minSpread;
            this.maxSpread = filters.maxSpread;
            this.minCostSpreadRatio = filters.minCostSpreadRatio;
            this.maxCostSpreadRatio = filters.maxCostSpreadRatio;
            if (filters.minExpirationDate) {
                this.minExpirationDate = filters.minExpirationDate;
            }
            if (filters.maxExpirationDate) {
                this.maxExpirationDate = filters.maxExpirationDate;
            }
            if (filters.maxBidAskSpreadPercent !== undefined) {
                this.maxBidAskSpreadPercent = filters.maxBidAskSpreadPercent;
            }
            if (filters.isCallSelected !== undefined) {
                this.isCallSelected = filters.isCallSelected;
            }
            if (filters.isDebitSelected !== undefined) {
                this.isDebitSelected = filters.isDebitSelected;
            }
            console.log("loaded find filters", filters);
        } else {
            console.log("no find filters found");
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
        
        const selectedLegExpiration = this.selectedLegs[0].option.expiration
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
            let option = this.options.find(x => x.optionType == leg.option.optionType && x.strikePrice == leg.option.strikePrice && x.expiration == this.uniqueExpirations[newIndex])
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
    candidateOptions : OptionDefinition[] = []
    
    isModalVisible: boolean = false
    createOrder() {
        this.isModalVisible = true
    }
    
    positionCreated() {
        this.isModalVisible = false
    }

    findSpreads(spreadType: SpreadType) {
        this.saveFindFiltersAndFindSpreads();
        
        let spreads: SpreadCandidate[] = [];
        const isCall = spreadType === SpreadType.DEBIT_CALL || spreadType === SpreadType.CREDIT_CALL;
        const isDebit = spreadType === SpreadType.DEBIT_CALL || spreadType === SpreadType.DEBIT_PUT;
        
        // Filter options based on option type and other criteria
        let filteredOptions = this.options.filter(x =>
            x.optionType == (isCall ? "Call" : "Put")
            && x.openInterest > 0
            && x.volume > 0
            && Math.abs(x.bid - x.ask) <= (this.maxBidAskSpreadPercent / 100) * x.mark
            && Date.parse(x.expiration) > Date.parse(this.minExpirationDate)
            && Date.parse(x.expiration) < Date.parse(this.maxExpirationDate));
            
        this.candidateOptions = filteredOptions;
        
        // Set up parameters based on spread type
        const longIsLowerStrike = isDebit === isCall;

        const processedPairs = new Set<string>();
        
        // Loop through options to find spreads
        for (let firstOption of filteredOptions) {
            for (let secondOption of filteredOptions) {
                // Determine long and short legs based on strike and spread type
                const longOption = longIsLowerStrike ? 
                    (firstOption.strikePrice < secondOption.strikePrice ? firstOption : secondOption) :
                    (firstOption.strikePrice > secondOption.strikePrice ? firstOption : secondOption);
                    
                const shortOption = longOption === firstOption ? secondOption : firstOption;
                
                // Create a unique identifier for this pair, regardless of order
                // Use option IDs if available, otherwise create a composite key using strikes and expiration
                const pairId = `${Math.min(longOption.strikePrice, shortOption.strikePrice)}-${Math.max(longOption.strikePrice, shortOption.strikePrice)}-${longOption.expiration}`;
                
                // Skip if we've already processed this pair
                if (processedPairs.has(pairId)) {
                    continue;
                }
                
                // Add to processed set
                processedPairs.add(pairId);
                
                // Check if this is a valid pair
                const validPair =
                    ((longIsLowerStrike && longOption.strikePrice < shortOption.strikePrice) ||
                    (!longIsLowerStrike && longOption.strikePrice > shortOption.strikePrice)) &&
                    Date.parse(longOption.expiration) === Date.parse(shortOption.expiration);
                    
                if (validPair) {
                    // Calculate cost/credit and spread
                    const priceDifference = longOption.mark - shortOption.mark;
                    const spread = Math.abs(longOption.strikePrice - shortOption.strikePrice);
                    
                    // For debit spreads, cost must be positive; for credit spreads, credit must be positive
                    if ((isDebit && priceDifference > 0) || (!isDebit && priceDifference < 0)) {
                        const costOrCredit = isDebit ? priceDifference : -priceDifference;
                        
                        // Apply filters
                        if (costOrCredit / spread >= this.minCostSpreadRatio 
                            && costOrCredit / spread <= this.maxCostSpreadRatio
                            && spread >= this.minSpread
                            && spread <= this.maxSpread) {
                            spreads.push({ longOption, shortOption, cost: costOrCredit });
                        }
                    }
                }
            }
        }
        
        // Sort based on spread type
        this.builtSpreads = isDebit ? this.sortDebitSpreads(spreads) : this.sortCreditSpreads(spreads);
    }

    toggleSpreadFindUI() {
        this.manualSelection = false;
    }

    toggleManualUI() {
        this.manualSelection = true;
    }

    // Utility method for sorting credit spreads
    sortCreditSpreads(spreads: SpreadCandidate[]): SpreadCandidate[] {
        return spreads.sort((a, b) => {
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
        });
    }

    // Utility method for sorting debit spreads
    sortDebitSpreads(spreads: SpreadCandidate[]): SpreadCandidate[] {
        return spreads.sort((a, b) => {
            // Maximum profit is the difference in strikes minus the cost
            let aSpreadWidth = Math.abs(a.longOption.strikePrice - a.shortOption.strikePrice)
            let bSpreadWidth = Math.abs(b.longOption.strikePrice - b.shortOption.strikePrice)
            
            let aMaxProfit = aSpreadWidth - a.cost
            let bMaxProfit = bSpreadWidth - b.cost

            // Return on Investment (ROI) = maxProfit / cost
            let aROI = aMaxProfit / a.cost
            let bROI = bMaxProfit / b.cost

            if (aROI === bROI) {
                // If ROI is the same, prefer the spread with lower cost
                return a.cost - b.cost
            } else {
                // Sort by ROI in descending order (higher is better)
                return bROI - aROI
            }
        });
    }

}
