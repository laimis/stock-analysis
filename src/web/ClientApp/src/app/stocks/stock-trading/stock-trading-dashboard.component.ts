import {Component, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute} from '@angular/router';
import {
    BrokerageAccount, OutcomeValueTypeEnum,
    PositionInstance, StockQuote,
    StockTradingPositions,
    StockViolation
} from '../../services/stocks.service';
import {GetErrors, GetStrategies, isLongTermStrategy, toggleVisuallyHidden} from "../../services/utils";
import {StockPositionsService} from "../../services/stockpositions.service";
import {stockOpenPositionExportLink} from "../../services/links.service";
import {CurrencyPipe, DecimalPipe, PercentPipe} from "@angular/common";

@Component({
    selector: 'app-stock-trading-dashboard',
    templateUrl: './stock-trading-dashboard.component.html',
    styleUrls: ['./stock-trading-dashboard.component.css'],
    providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class StockTradingDashboardComponent implements OnInit {
    positions: PositionInstance[]
    sortedPositions: PositionInstance[]
    loaded: boolean = false
    loading: boolean = true
    activeTab: string = 'positions'
    violations: StockViolation[]
    brokerageAccount: BrokerageAccount
    errors: string[]
    quotes: Map<string, StockQuote>
    strategies: { key: string; value: string }[] = []
    metricToRender: string
    metricFunc: (p: PositionInstance) => any;
    metricType: OutcomeValueTypeEnum;


    private NO_LONG_TERM_STRATEGY = "nolongterm"
    private NONE = ""
    private SHORTS = "shorts"
    private LONGS = "longs"
    private RR = "rr"
    private UnrealizedRR = "unrealizedRR"

    strategyToFilter = this.NO_LONG_TERM_STRATEGY


    sortOptions: { name: string; value: string }[] = [
        {value: this.RR, name: "R/R"},
        {value: this.UnrealizedRR, name: "Unrealized R/R"},
        {value: "pl", name: "P/L"},
        {value: "plPercent", name: "P/L %"},
        {value: "plUnrealized", name: "Unrealized P/L"},
        {value: "plUnrealizedPercent", name: "Unrealized P/L %"},
        {value: "cost", name: "Cost"},
        {value: "ticker", name: "Ticker"},
        {value: "daysSinceLastTransaction", name: "Days Since Last Transaction"},
        {value: "riskedAmount", name: "Risked Amount"},
        {value: "riskedAmountFromStop", name: "Risked Amount from Stop"},
        {value: "percentToStopFromCost", name: "% to Stop from Cost"},
        {value: "daysHeld", name: "Days Held"},
    ]

    constructor(
        private stockService: StockPositionsService,
        private title: Title,
        private route: ActivatedRoute,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {
    }

    ngOnInit() {
        this.route.params.subscribe(param => {
            this.activeTab = param['tab'] || 'positions'
        })

        this.title.setTitle("Trading Dashboard - Nightingale Trading")
        this.loadEntries()
    }

    getOpenPositionExportLink() {
        return stockOpenPositionExportLink()
    }

    matchesStrategyCheck(p: PositionInstance, strategy: string) {
        return strategy === "" ?
            p.labels.findIndex(l => l.key === "strategy") === -1 :
            p.labels.findIndex(l => l.key === "strategy" && l.value === strategy) !== -1
    }

    isActive(tabName: string) {
        return tabName == this.activeTab
    }

    activateTab(tabName: string) {
        this.activeTab = tabName
    }

    refresh() {
        this.loadEntries()
    }

    strategyToFilterChanged = (elem: EventTarget) => {
        this.strategyToFilter = (elem as HTMLInputElement).value
        this.updatePositions()
    }

    private loadEntries() {
        this.loading = true
        this.stockService.getTradingEntries().subscribe((r: StockTradingPositions) => {
            this.positions = r.current
            this.violations = r.violations
            this.brokerageAccount = r.brokerageAccount
            this.quotes = r.prices
            this.loading = false
            this.loaded = true

            // create an array of strategies where value is the stratey name and count of positions that match

            let stratsWithCounts = GetStrategies().map(
                (s) => {
                    var count = this.positions.filter(i => this.matchesStrategyCheck(i, s.key)).length
                    return {key: s.key, value: s.value + " - " + count}
                }
            )
            this.strategies.push({key: "all", value: "All - " + this.positions.length})

            let longTermPositions = this.positions.filter(
                (p) => {
                    let strategy = p.labels.find(l => l.key == 'strategy')
                    return strategy && isLongTermStrategy(strategy.value)
                }
            )

            let shorts = this.positions.filter((p) => p.isShort)
            let longs = this.positions.filter((p) => !p.isShort)

            let noStrategy = this.positions.filter(i => this.matchesStrategyCheck(i, this.NONE))

            this.strategies.push({
                key: this.NO_LONG_TERM_STRATEGY,
                value: "All minus long term - " + (this.positions.length - longTermPositions.length)
            })
            this.strategies.push({key: this.LONGS, value: "Longs - " + longs.length})
            this.strategies.push({key: this.SHORTS, value: "Shorts - " + shorts.length})
            this.strategies.push({key: this.NONE, value: "None - " + noStrategy.length})
            this.strategies = this.strategies.concat(
                stratsWithCounts
            )

            this.metricToRender = this.UnrealizedRR
            
            this.metricChanged(this.metricToRender)
            
            this.updatePositions()

        }, err => {
            this.loading = false
            this.loaded = true
            console.log(err)
            this.errors = GetErrors(err)
        })
    }

    metricChanged(value: string) {

        console.log("metric changed to " + value)
        this.metricToRender = value

        switch (value) {
            case "pl":
                this.metricFunc = (p: PositionInstance) => p.profit
                this.metricType = OutcomeValueTypeEnum.Currency
                break;
            case "plPercent":
                this.metricFunc = (p: PositionInstance) => p.gainPct
                this.metricType = OutcomeValueTypeEnum.Percentage
                break;
            case "plUnrealized":
                this.metricFunc = (p: PositionInstance) => p.numberOfShares * (this.getPrice(p) - p.averageCostPerShare) + p.profit
                this.metricType = OutcomeValueTypeEnum.Currency
                break;
            case "plUnrealizedPercent":
                this.metricFunc = (p: PositionInstance) => (this.getPrice(p) - p.averageCostPerShare) / p.averageCostPerShare
                this.metricType = OutcomeValueTypeEnum.Percentage
                break;
            case "cost":
                this.metricFunc = (p: PositionInstance) => p.cost
                this.metricType = OutcomeValueTypeEnum.Currency
                break;
            case "ticker":
                this.metricFunc = (p: PositionInstance) => p.ticker
                this.metricType = OutcomeValueTypeEnum.String
                break
            case "daysSinceLastTransaction":
                this.metricFunc = (p: PositionInstance) => p.daysSinceLastTransaction
                this.metricType = OutcomeValueTypeEnum.Number
                break
            case "riskedAmount":
                this.metricFunc = (p: PositionInstance) => p.riskedAmount ? p.riskedAmount : 0
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            case "riskedAmountFromStop":
                this.metricFunc = (p: PositionInstance) => (p.stopPrice - p.averageCostPerShare) * p.numberOfShares
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            case "daysHeld":
                this.metricFunc = (p: PositionInstance) => p.daysHeld
                this.metricType = OutcomeValueTypeEnum.Number
                break
            case "unrealizedRR":
                this.metricFunc = (p: PositionInstance) => this.calculateUnrealizedRR(p)
                this.metricType = OutcomeValueTypeEnum.Number
                break
            case "percentToStopFromCost":
                this.metricFunc = (p: PositionInstance) => p.percentToStopFromCost
                this.metricType = OutcomeValueTypeEnum.Percentage
                break
            default:
                this.metricFunc = (p: PositionInstance) => p.rr
                this.metricType = OutcomeValueTypeEnum.Number
        }
    }


    getQuote(p: PositionInstance) {
        return this.quotes[p.ticker]
    }

    getPrice(p: PositionInstance) {
        if (this.quotes) {
            return this.quotes[p.ticker]?.price
        }
        return 0
    }

    calculateUnrealizedRR(p: PositionInstance) {
        return (p.profit + p.numberOfShares * (this.getPrice(p) - p.averageCostPerShare)) / (p.riskedAmount === 0 ? 40 : p.riskedAmount)
    }

    updatePositions() {

        console.log(this.metricToRender)

        this.sortedPositions = this.positions
            .filter(p => {
                if (this.strategyToFilter === "all") {
                    return true
                }

                let positionStrategy = p.labels.find(l => l.key === "strategy")
                if (!positionStrategy) {
                    return this.strategyToFilter === this.NONE
                }

                if (this.strategyToFilter === this.NO_LONG_TERM_STRATEGY) {
                    return !isLongTermStrategy(positionStrategy.value)
                }

                if (this.strategyToFilter === this.SHORTS) {
                    return p.isShort
                }

                if (this.strategyToFilter === this.LONGS) {
                    return !p.isShort
                }

                if (this.strategyToFilter === this.NONE) {
                    return this.matchesStrategyCheck(p, this.NONE)
                }

                return positionStrategy.value === this.strategyToFilter
            })
            .sort((a, b) => {
                if (Number.isFinite(this.metricFunc(a))) {
                    const bNumber = this.metricFunc(b)
                    const aNumber = this.metricFunc(a)
                    return bNumber - aNumber
                }
                return String(this.metricFunc(a)).localeCompare(String(this.metricFunc(b)))
            })
    }
    
    getMetricToRender(p: PositionInstance) {
        let val = this.metricFunc(p)
        if (Number.isFinite(val)) {
            val = Math.round(val * 100) / 100
        }

        if (this.metricType === OutcomeValueTypeEnum.Percentage) {
            return this.percentPipe.transform(val, '1.0-2')
        } else if (this.metricType === OutcomeValueTypeEnum.Currency) {
            return this.currencyPipe.transform(val)
        } else if (this.metricType === OutcomeValueTypeEnum.String) {
            return val
        } else {
            return this.decimalPipe.transform(val)
        }
    }


    invested: number = 0
    readonly toggleVisuallyHidden = toggleVisuallyHidden;
}

