import {Component, OnInit} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {OptionDefinition, OptionService, OptionSpread} from 'src/app/services/option.service';
import {GetErrors} from 'src/app/services/utils';

@Component({
    selector: 'app-option-chain',
    templateUrl: './option-chain.component.html',
    styleUrls: ['./option-chain.component.css'],
    standalone: false
})
export class OptionChainComponent implements OnInit {
    ticker: string;

    public options: OptionDefinition[]
    public filteredOptions: OptionDefinition[]
    public filteredOptionsWithBothSides: OptionDefinition[]
    public expirationMap: Array<OptionDefinition[]>
    public stockPrice: number
    public expirations: string[]
    public loading: boolean = true

    public expirationSelection: string = ""
    public sideSelection: string = ""
    public minBid: number = 0
    public maxAsk: number = 0
    public minStrikePrice: number = 0
    public maxStrikePrice: number = 0

    volatility: number;
    numberOfContracts: number;

    straddles: OptionSpread[] = [];
    bullCallSpreads: OptionSpread[] = [];
    bullPutSpreads: OptionSpread[] = [];
    bearCallSpreads: OptionSpread[] = [];
    bearPutSpreads: OptionSpread[] = [];

    errors: string[] = [];
    selectedSpread: string = null

    constructor(
        private optionService: OptionService,
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        var ticker = this.route.snapshot.paramMap.get('ticker');
        if (ticker) {
            this.ticker = ticker;
        }

        this.optionService.getOptionChain(this.ticker).subscribe(result => {
            this.volatility = result.volatility
            this.numberOfContracts = result.numberOfContracts
            this.options = result.options
            this.expirations = result.expirations
            this.stockPrice = result.stockPrice
            this.sideSelection = "put"
            this.minBid = 0
            this.maxAsk = 0
            this.runFilter()
        }, error => {
            this.errors = GetErrors(error)
            this.loading = false
        })
    }

    putOpenInterest() {
        return this.options.filter(x => x.optionType == "put").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    callOpenInterest() {
        return this.options.filter(x => x.optionType == "call").map(x => x.openInterest).reduce((a, b) => a + b, 0)
    }

    putVolume() {
        return this.options.filter(x => x.optionType == "put").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    callVolume() {
        return this.options.filter(x => x.optionType == "call").map(x => x.volume).reduce((a, b) => a + b, 0)
    }

    runFilter() {
        console.log("running filter")
        console.log("expiration: " + this.expirationSelection)
        console.log("side: " + this.sideSelection)
        console.log("min bid: " + this.minBid)
        console.log("max ask: " + this.maxAsk)
        console.log("min strike price: " + this.minStrikePrice)
        console.log("max strike price: " + this.maxStrikePrice)

        this.filteredOptions = this.options.filter(opt => this.includeOption(opt, true), this);
        this.filteredOptionsWithBothSides = this.options.filter(opt => this.includeOption(opt, false), this);
        this.loading = false;

        let expirationMap = new Map<string, OptionDefinition[]>();
        this.filteredOptions.forEach(function (value, index, arr) {
            if (!expirationMap.has(value.expirationDate)) {
                expirationMap.set(value.expirationDate, [value])
            } else {
                var temp = expirationMap.get(value.expirationDate)
                temp.push(value)
            }
        })
        this.expirationMap = Array.from(expirationMap.values());

        this.bullCallSpreads = this.optionService.findBullCallSpreads(this.filteredOptionsWithBothSides)
        this.bearCallSpreads = this.optionService.findBearCallSpreads(this.filteredOptionsWithBothSides)
        this.straddles = this.optionService.findStraddles(this.filteredOptionsWithBothSides)
        this.bullPutSpreads = this.optionService.findBullPutSpreads(this.filteredOptionsWithBothSides)
        this.bearPutSpreads = this.optionService.findBearPutSpreads(this.filteredOptionsWithBothSides)

        console.log("filter running finished")
    }

    includeOption(element: OptionDefinition, includeSizeCheck: boolean) {
        if (this.expirationSelection !== "") {
            if (element.expirationDate != this.expirationSelection) {
                console.log("filterig out expiration " + element.expirationDate)
                return false
            }
        }

        if (this.sideSelection !== "" && includeSizeCheck) {
            if (element.optionType != this.sideSelection) {
                console.log("filterig out side " + element.optionType)
                return false
            }
        }

        if (element.bid < this.minBid) {
            console.log("filtering out min price " + element.bid)
            return false
        }

        if (element.ask > this.maxAsk && this.maxAsk != 0) {
            console.log("filtering out max price " + element.ask)
            return false
        }

        if (this.minStrikePrice > 0) {
            if (element.strikePrice < this.minStrikePrice) {
                return false
            }
        }

        if (this.maxStrikePrice > 0) {
            if (element.strikePrice > this.maxStrikePrice) {
                return false
            }
        }

        return true
    }

    selectSpreads(event, type) {
        this.selectedSpread = type
        console.log("selected spread: " + this.selectedSpread)
        event.preventDefault()
    }

    onExpirationChange(newValue) {
        console.log(newValue)
        this.expirationSelection = newValue
        this.runFilter()
    }

    onSideChange(newValue) {
        console.log(newValue)
        this.sideSelection = newValue
        this.runFilter()
    }

    onMinBidChange() {
        console.log(this.minBid)
        this.runFilter()
    }

    onMaxAskChange() {
        console.log(this.maxAsk)
        this.runFilter()
    }

    onMinStrikePriceChange() {
        console.log(this.minStrikePrice)
        this.runFilter()
    }

    onMaxStrikePriceChange() {
        console.log(this.maxStrikePrice)
        this.runFilter()
    }

    setStrikePriceFilterNearMoney() {
        let closestPriceFunc = function (target, prev, curr) {
            return (Math.abs(curr - target) < Math.abs(prev - target) ? curr : prev);
        }

        let strikePrices = this.options.map(x => x.strikePrice)

        let minStrikePrice = Math.floor(this.stockPrice - this.stockPrice * 0.1)
        let closestStrikePrice = strikePrices.reduce((prev, curr) => closestPriceFunc(minStrikePrice, prev, curr));
        this.minStrikePrice = closestStrikePrice

        let maxStrikePrice = Math.ceil(this.stockPrice + this.stockPrice * 0.1)
        closestStrikePrice = strikePrices.reduce((prev, curr) => closestPriceFunc(maxStrikePrice, prev, curr));
        this.maxStrikePrice = closestStrikePrice

        this.runFilter()
    }

    getStrikePrices(options: OptionDefinition[]) {
        return options.map(x => x.strikePrice.toString())
    }

    getOpenInterest(options: OptionDefinition[]) {
        return options.map(x => x.openInterest)
    }

    getVolume(options: OptionDefinition[]) {
        return options.map(x => x.volume)
    }

    getOptionsForExpiration(expiration: string) {
        return this.options.filter(x => x.expirationDate == expiration)
    }

    isSpreadHealthy(option: OptionDefinition) {
        return option.bid > 0 && option.ask > 0 && ((option.ask - option.bid) / option.ask <= 0.1)
    }

    itm(option: OptionDefinition) {
        if (option.optionType == "call") {
            return this.stockPrice > option.strikePrice
        }

        if (option.optionType == "put") {
            return this.stockPrice < option.strikePrice
        }

        throw new Error("Invalid option type: " + option.optionType)
    }
}
