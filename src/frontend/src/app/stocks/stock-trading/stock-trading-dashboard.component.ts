import { Component, OnInit, inject } from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {
    AccountStatus,
    BrokerageAccount, BrokerageAccountSnapshot,
    OutcomeValueTypeEnum,
    StockPosition,
    StockQuote,
    StockTradingPositions,
    StockViolation
} from '../../services/stocks.service';
import {GetErrors, GetStockStrategies, isLongTermStrategy, toggleVisuallyHidden} from "../../services/utils";
import {StockPositionsService} from "../../services/stockpositions.service";
import { StockTradingPositionsComponent } from './stock-trading-positions.component';
import { StockTradingSummaryComponent } from './stock-trading-summary.component';
import { StockTradingChartsComponent } from './stock-trading-charts.component';
import { StockViolationsComponent } from './stock-violations.component';
import { StockTradingReviewComponent } from '../../stocks/stock-trading-review/stock-trading-review.component';
import {stockOpenPositionExportLink} from "../../services/links.service";
import {GlobalService} from "../../services/global.service";
import { ErrorDisplayComponent } from "src/app/shared/error-display/error-display.component";
import { NgClass } from '@angular/common';

@Component({
    selector: 'app-stock-trading-dashboard',
    templateUrl: './stock-trading-dashboard.component.html',
    styleUrls: ['./stock-trading-dashboard.component.css'],
    imports: [ErrorDisplayComponent, NgClass, RouterLink, StockTradingPositionsComponent, StockTradingSummaryComponent, StockTradingChartsComponent, StockViolationsComponent, StockTradingReviewComponent]
})
export class StockTradingDashboardComponent implements OnInit {
    private globalService = inject(GlobalService);
    private stockService = inject(StockPositionsService);
    private route = inject(ActivatedRoute);

    balances: BrokerageAccountSnapshot[]
    userState: AccountStatus
    positions: StockPosition[]
    sortedPositions: StockPosition[]
    loaded: boolean = false
    loading: boolean = true
    activeTab: string = 'positions'
    violations: StockViolation[]
    brokerageAccount: BrokerageAccount
    errors: string[]
    quotes: Map<string, StockQuote>
    strategies: { key: string; value: string }[] = []
    metricToRender: string
    metricFunc: (p: StockPosition) => any;
    metricType: OutcomeValueTypeEnum;
    invested: number = 0
    readonly toggleVisuallyHidden = toggleVisuallyHidden;
    strategyToFilter = "all"
    private NONE = ""
    private SHORTS = "shorts"
    private LONGS = "longs"
    private RR = "rr"
    private UnrealizedRR = "unrealizedRR"
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

    ngOnInit() {
        this.route.params.subscribe(param => {
            this.activeTab = param['tab'] || 'positions'
        })
        
        this.globalService.accountStatusFeed.subscribe(s => {
            this.userState = s
        })

        this.loadEntries()
    }

    getOpenPositionExportLink() {
        return stockOpenPositionExportLink()
    }

    matchesStrategyCheck(p: StockPosition, strategy: string) {
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

    getPrice(p: StockPosition) {
        if (this.quotes) {
            return this.quotes[p.ticker]?.price
        }
        return 0
    }

    calculateUnrealizedRR(p: StockPosition) {
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

    private loadEntries() {
        this.loading = true
        this.stockService.getTradingEntries().subscribe(
            
            (r: StockTradingPositions) => {
            this.positions = r.current
            this.violations = r.violations
            this.brokerageAccount = r.brokerageAccount
            this.quotes = r.prices
            this.balances = r.dailyBalances
            this.loading = false
            this.loaded = true

            // create an array of strategies where value is the stratey name and count of positions that match

            let stratsWithCounts = GetStockStrategies().map(
                (s) => {
                    const count = this.positions.filter(i => this.matchesStrategyCheck(i, s.key)).length;
                    return {key: s.key, value: s.value + " - " + count}
                }
            )
            this.strategies.push({key: "all", value: "All - " + this.positions.length})

            let shorts = this.positions.filter((p) => p.isShort)
            let longs = this.positions.filter((p) => !p.isShort)

            let noStrategy = this.positions.filter(i => this.matchesStrategyCheck(i, this.NONE))

            this.strategies.push({key: this.LONGS, value: "Longs - " + longs.length})
            this.strategies.push({key: this.SHORTS, value: "Shorts - " + shorts.length})
            this.strategies.push({key: this.NONE, value: "None - " + noStrategy.length})
            this.strategies = this.strategies.concat(
                stratsWithCounts
            )

            this.metricToRender = this.UnrealizedRR

            this.updatePositions()

        }, 
        (err) => {
            this.loading = false
            this.loaded = true
            console.log(err)
            this.errors = GetErrors(err)
        })
    }
}

