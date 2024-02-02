import {Component, EventEmitter, Input, Output} from '@angular/core';
import {GetStrategies, isLongTermStrategy, toggleVisuallyHidden} from 'src/app/services/utils';
import {
  BrokerageOrder,
  OutcomeValueTypeEnum,
  PositionInstance,
  StockQuote
} from '../../services/stocks.service';
import {CurrencyPipe, DecimalPipe, PercentPipe} from '@angular/common';


@Component({
  selector: 'app-stock-trading-positions',
  templateUrl: './stock-trading-positions.component.html',
  styleUrls: ['./stock-trading-positions.component.css'],
  providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class StockTradingPositionsComponent {
    sortedPositions: PositionInstance[];
    _positions: PositionInstance[];
    metricFunc: (p: PositionInstance) => any;
    metricType: OutcomeValueTypeEnum;
    strategies: { key: string; value: string }[] = []

    private NO_LONG_TERM_STRATEGY = "nolongterm"
    private NONE = ""
    private SHORTS = "shorts"
    private LONGS = "longs"
    private RR = "rr"
    private UnrealizedRR = "unrealizedRR"

    metricToRender: string
    strategyToFilter = this.NO_LONG_TERM_STRATEGY

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

        let longTermPositions = input.filter(
            (p) => {
                let strategy = p.labels.find(l => l.key == 'strategy')
                return strategy && isLongTermStrategy(strategy.value)
            }
        )

        let shorts = input.filter((p) => p.isShort)
        let longs = input.filter((p) => !p.isShort)

        let noStrategy = input.filter(i => this.matchesStrategyCheck(i, this.NONE))

        this.strategies.push({key: this.NO_LONG_TERM_STRATEGY, value: "All minus long term - " + (input.length - longTermPositions.length)})
        this.strategies.push({key: this.LONGS, value: "Longs - " + longs.length})
        this.strategies.push({key: this.SHORTS, value: "Shorts - " + shorts.length})
        this.strategies.push({key: this.NONE, value: "None - " + noStrategy.length})
        this.strategies = this.strategies.concat(
            stratsWithCounts
        )
        this.metricChanged(this.UnrealizedRR)
    }

    @Input()
    orders:BrokerageOrder[];
    
    private _quotes : Map<string, StockQuote>;
    @Input()
    set quotes(val:Map<string, StockQuote>) {
      this._quotes = val
      this.updatePositions()
    }
    get quotes() {
      return this._quotes
    }
    
    @Output()
    brokerageOrderEntered = new EventEmitter<string>()
    sendBrokerageOrderEntered($event:string) {
        this.brokerageOrderEntered.emit($event)
    }

    // constructor that takes stock service
    constructor(
        private percentPipe: PercentPipe,
        private currencyPipe: CurrencyPipe,
        private decimalPipe: DecimalPipe
    ) {}

    toggleVisibility(elem:HTMLElement) {
        toggleVisuallyHidden(elem)
    }

    getQuote(p:PositionInstance) {
      return this.quotes[p.ticker]
    }

    getPrice(p:PositionInstance) {
      if (this.quotes) {
        return this.quotes[p.ticker].price
      }
      return 0
    }

    sortOptions: { name: string; value: string }[] = [
        { value: this.RR, name: "R/R" },
        { value: this.UnrealizedRR, name: "Unrealized R/R" },
        { value: "pl", name: "P/L" },
        { value: "plPercent", name: "P/L %" },
        { value: "plUnrealized", name: "Unrealized P/L" },
        { value: "plUnrealizedPercent", name: "Unrealized P/L %" },
        { value: "cost", name: "Cost" },
        { value: "ticker", name: "Ticker" },
        { value: "daysSinceLastTransaction", name: "Days Since Last Transaction" },
        { value: "riskedAmount", name: "Risked Amount" },
        { value: "riskedAmountFromStop", name: "Risked Amount from Stop" },
        { value: "percentToStopFromCost", name: "% to Stop from Cost" },
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

    strategyToFilterChanged = (elem: EventTarget) => {
      this.strategyToFilter = (elem as HTMLInputElement).value
      this.updatePositions()
    }

    metricChanged(value:string) {

      console.log("metric changed to " + value)
      this.metricToRender = value

      switch (value) {
          case "pl":
              this.metricFunc = (p:PositionInstance) => p.profit
              this.metricType = OutcomeValueTypeEnum.Currency
              break;
          case "plPercent":
              this.metricFunc = (p:PositionInstance) => p.gainPct
              this.metricType = OutcomeValueTypeEnum.Percentage
              break;
          case "plUnrealized":
              this.metricFunc = (p:PositionInstance) => p.numberOfShares * (this.getPrice(p) - p.averageCostPerShare) + p.profit
              this.metricType = OutcomeValueTypeEnum.Currency
              break;
          case "plUnrealizedPercent":
              this.metricFunc = (p:PositionInstance) => (this.getPrice(p) - p.averageCostPerShare) / p.averageCostPerShare
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
          case this.UnrealizedRR:
              this.metricFunc = (p:PositionInstance) => this.calculateUnrealizedRR(p)
              this.metricType = OutcomeValueTypeEnum.Number
              break
        case "percentToStopFromCost":
              this.metricFunc = (p:PositionInstance) => p.percentToStopFromCost
              this.metricType = OutcomeValueTypeEnum.Percentage
              break
          default:
              this.metricFunc = (p:PositionInstance) => p.rr
              this.metricType = OutcomeValueTypeEnum.Number
      }

      this.updatePositions()
    }

    getMetricToRender(p:PositionInstance) {
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

    calculateUnrealizedRR(p:PositionInstance) {
      return (p.profit + p.numberOfShares * (this.getPrice(p) - p.averageCostPerShare)) / (p.riskedAmount === 0 ? 40 : p.riskedAmount)
    }

    matchesStrategyCheck(p:PositionInstance, strategy:string) {
      return strategy === this.NONE ?
        p.labels.findIndex(l => l.key === "strategy") === -1 :
        p.labels.findIndex(l => l.key === "strategy" && l.value === strategy) !== -1
    }

    updatePositions() {

      console.log(this.metricToRender)

      this.sortedPositions = this._positions
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
            return String(this.getMetricToRender(a)).localeCompare(String(this.getMetricToRender(b)))
          })
    }
}

