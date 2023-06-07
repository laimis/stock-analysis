import { Component, Input } from '@angular/core';
import { GetStrategies, toggleVisuallyHidden } from 'src/app/services/utils';
import { BrokerageOrder, OutcomeValueTypeEnum, PositionInstance, StocksService } from '../../services/stocks.service';
import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';

@Component({
  selector: 'app-stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css'],
  providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class StockTradingPositionsComponent {
    sortedPositions: PositionInstance[];
    _positions: PositionInstance[];
    metricToRender: string = "rr"
    metricFunc: (p: PositionInstance) => any = (p:PositionInstance) => p.rr;
    metricType: OutcomeValueTypeEnum = OutcomeValueTypeEnum.Number
    candidateRiskAmount: number = 0
    candidateStopPrice: number = 0
    strategies: { key: string; value: string }[] = []

    @Input()
    set positions(input: PositionInstance[]) {
        this._positions = input

        // create an array of strategies where value is the stratey name and count of positions that match
        
        let stratsWithCounts = GetStrategies().map(
            (s) => {
                var count = input.filter(i => this.matchesStrategyCheck(i, s.key)).length
                return {key: s.key, value: s.value + " - " + count}
            }
        )
        this.strategies.push({key: "all", value: "All - " + input.length})
        this.strategies = this.strategies.concat(
            stratsWithCounts
        )
        this.updatePositions()
    }

    @Input()
    orders:BrokerageOrder[];

    // constructor that takes stock service
    constructor(
        private stockService:StocksService,
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {}
    
    toggleVisibility(elem:HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    setCandidateValues(p:PositionInstance) {
        this.candidateRiskAmount = p.riskedAmount
        this.candidateStopPrice = p.stopPrice
    }

    recalculateRiskAmount(p:PositionInstance) {
        var newRiskAmount = (p.averageCostPerShare - this.candidateStopPrice) * p.numberOfShares
        this.candidateRiskAmount = newRiskAmount
        p.riskedAmount = newRiskAmount
    }

    setStopPrice(p:PositionInstance) {
        this.stockService.setStopPrice(p.ticker, this.candidateStopPrice).subscribe(
            (_) => {
                p.stopPrice = this.candidateStopPrice
            }
        )
    }

    setRiskAmount(p:PositionInstance) {
        this.stockService.setRiskAmount(p.ticker, p.positionId, this.candidateRiskAmount).subscribe(
            (_) => {
                p.riskedAmount = this.candidateRiskAmount
            }
        )
    }

    sortOptions: { name: string; value: string }[] = [
        { value: "rr", name: "R/R" },
        { value: "pl", name: "P/L" },
        { value: "unrealizedPL", name: "Unrealized P/L" },
        { value: "combinedPL", name: "Combined P/L" },
        { value: "plPercent", name: "P/L %" },
        { value: "cost", name: "Cost" },
        { value: "ticker", name: "Ticker" },
        { value: "percentToStop", name: "% to Stop" },
        { value: "daysSinceLastTransaction", name: "Days Since Last Transaction" },
        { value: "riskedAmount", name: "Risked Amount" },
        { value: "riskedAmountFromStop", name: "Risked Amount from Stop" },
        { value: "daysHeld", name: "Days Held" },
    ]

    renderStyle: string = "card"  // other style is "table"
    renderStyleName: string = "Card layout"
    toggleRenderStyle() {
        if (this.renderStyle == "card") {
            this.renderStyle = "table"
            this.renderStyleName = "Card layout"
        } else {
            this.renderStyle = "card"
            this.renderStyleName = "Table layout"
        }
    }

    excludeLongTerm: boolean = true
    excludeLongTermLabels = {true: "No long term", false: "With long term"}
    excludeLongTermName: string = this.excludeLongTermLabels[this.excludeLongTerm.toString().toLowerCase()]
    toggleExcludeLongTerm() {
        this.excludeLongTerm = !this.excludeLongTerm
        this.excludeLongTermName = this.excludeLongTermLabels[this.excludeLongTerm.toString().toLowerCase()]
        this.updatePositions()
    }

    strategyToFilter = "all"
    strategyToFilterChanged = (elem: EventTarget) => {
        var value = (elem as HTMLInputElement).value
        this.strategyToFilter = value
        this.updatePositions()
    }

    metricChanged(elem: EventTarget) {
        var value = (elem as HTMLInputElement).value
        
        this.metricToRender = value

        switch (value) {
            case "pl":
                this.metricFunc = (p:PositionInstance) => p.profit
                this.metricType = OutcomeValueTypeEnum.Currency
                break;
            case "plPercent":
                this.metricFunc = (p:PositionInstance) => p.unrealizedGainPct
                this.metricType = OutcomeValueTypeEnum.Percentage
                break;
            case "cost":
                this.metricFunc = (p:PositionInstance) => p.cost
                this.metricType = OutcomeValueTypeEnum.Currency
                break;
            case "ticker":
                this.metricFunc = (p:PositionInstance) => p.ticker
                this.metricType = OutcomeValueTypeEnum.String
                break
            case "daysSinceLastTransaction":
                this.metricFunc = (p:PositionInstance) => p.daysSinceLastTransaction
                this.metricType = OutcomeValueTypeEnum.Number
                break
            case "percentToStop":
                this.metricFunc = (p:PositionInstance) => p.percentToStop
                this.metricType = OutcomeValueTypeEnum.Percentage
                break
            case "riskedAmount":
                this.metricFunc = (p:PositionInstance) => p.riskedAmount ? p.riskedAmount : 0
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            case "riskedAmountFromStop":
                this.metricFunc = (p:PositionInstance) => (p.stopPrice - p.averageCostPerShare) * p.numberOfShares
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            case "daysHeld":
                this.metricFunc = (p:PositionInstance) => p.daysHeld
                this.metricType = OutcomeValueTypeEnum.Number
                break
            case "unrealizedPL":
                this.metricFunc = (p:PositionInstance) => p.unrealizedProfit
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            case "combinedPL":
                this.metricFunc = (p:PositionInstance) => p.combinedProfit
                this.metricType = OutcomeValueTypeEnum.Currency
                break
            default:
                this.metricFunc = (p:PositionInstance) => p.rr
                this.metricType = OutcomeValueTypeEnum.Number
        }

        this.updatePositions()
    }

    getMetricToRender(p:PositionInstance) {
        var val = this.metricFunc(p)
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

    matchesStrategyCheck(p:PositionInstance, strategy:string) {
        if (strategy === "all") {
            return true
        }
        return p.labels.findIndex(l => l.key === "strategy" && l.value === strategy) !== -1
    }

    matchesStrategy = (p:PositionInstance) => {
        return this.matchesStrategyCheck(p, this.strategyToFilter)
    }

    updatePositions() {
        var positions = this._positions.sort((a, b) => {
            if (Number.isFinite(this.metricFunc(a))) {
                return this.metricFunc(b) - this.metricFunc(a)
            }
            return String(this.metricFunc(a)).localeCompare(String(this.metricFunc(b)))
        })

        if (this.excludeLongTerm) {
            positions = positions.filter(p => this.matchesStrategyCheck(p, "longterm") === false)
        }

        if (this.strategyToFilter !== "all") {
            positions = positions.filter(this.matchesStrategy)
        }

        this.sortedPositions = positions
    }
}

