import {Component, Input} from '@angular/core';
import {
  BrokerageOrder,
  PositionEvent,
  PositionInstance, StockQuote,
  StocksService,
  StrategyProfitPoint
} from '../../services/stocks.service';
import {Output} from '@angular/core';
import {EventEmitter} from '@angular/core';
import {GetErrors, GetStrategies, toggleVisuallyHidden} from 'src/app/services/utils';
import {StockPositionsService} from "../../services/stockpositions.service";

@Component({
  selector: 'app-stock-trading-position',
  templateUrl: './stock-trading-position.component.html',
  styleUrls: ['./stock-trading-position.component.css']
})
export class StockTradingPositionComponent {
  candidateRiskAmount: number = 0
  candidateStopPrice: number = 0
  _position: PositionInstance;
  numberOfProfitPoints: number = 4

  positionProfitPoints: StrategyProfitPoint[] = []
  positionStrategy: string = null
  positionOrders: BrokerageOrder[] = [];
  allOrders: BrokerageOrder[] = [];
  strategies: { key: string; value: string; }[];
  showOrderForm: boolean = false;

  @Input()
  set position(v: PositionInstance) {
    this._position = v

    if (this._position) {
      this.positionStrategy = v.labels.find(l => l.key == "strategy")?.value
      this.positionProfitPoints = []
      this.setCandidateValues()
      this.updatePositionOrders()
    }
  }

  @Input()
  set orders(value: BrokerageOrder[]) {
    this.allOrders = value;
    this.updatePositionOrders();
  }

  @Input()
  quote: StockQuote

  updatePositionOrders() {
    if (!this._position) {
      return
    }

    if (!this.allOrders) {
      return
    }

    this.positionOrders = this.allOrders.filter(o => o.ticker == this._position.ticker)
  }

  @Output()
  positionDeleted = new EventEmitter()
    
    @Output()
    brokerageOrdersChanged = new EventEmitter<string>()
    
  // constructor that takes stock service
  constructor(
    private stockService: StockPositionsService
  ) {
    this.strategies = GetStrategies()
  }

  fetchProfitPoints() {
    this.stockService.getStrategyProfitPoints(
      this._position.positionId,
      this.numberOfProfitPoints).subscribe(
      (profitPoints) => {
        this.positionProfitPoints = profitPoints
      }
    )
  }

  profitPointReached(price: number) {
    if (this._position.isShort) {
      return price >= this.quote.price
    }

    return price <= this.quote.price
  }

  setCandidateValues() {
    // round risked amount to 2 decimal places
    this.candidateRiskAmount = Math.round(this._position.riskedAmount * 100) / 100
    this.candidateStopPrice = Math.round(this._position.stopPrice * 100) / 100
  }

  recalculateRiskAmount() {
    const newRiskAmount = (this._position.averageCostPerShare - this.candidateStopPrice) * this._position.numberOfShares;
    this.candidateRiskAmount = newRiskAmount
    this._position.riskedAmount = newRiskAmount
    return false
  }

  setStopPrice(elementVisibilityToToggle:HTMLElement[]) {
    this.stockService.setStopPrice(this._position.positionId, this.candidateStopPrice).subscribe(
      (_) => {
        this._position.stopPrice = this.candidateStopPrice
        elementVisibilityToToggle.forEach(this.toggleVisibility)
      }
    )
  }

  deleteStopPrice() {
    if (confirm("Are you sure you want to delete the stop price?")) {
      this.stockService.deleteStopPrice(this._position.positionId).subscribe(
        (_) => {
          this._position.stopPrice = null
          this._position.riskedAmount = null
        }
      )
    }

    return false
  }

  setRiskAmount(elementVisibilityToToggle:HTMLElement[]) {
    if (confirm("Are you sure you want to set the risk amount?")) {
      this.stockService.setRiskAmount(this._position.positionId, this.candidateRiskAmount).subscribe(
        (_) => {
          this._position.riskedAmount = this.candidateRiskAmount
          elementVisibilityToToggle.forEach(this.toggleVisibility)
        }
      )
    }
  }

  getCssClassForEvent(e: PositionEvent) {
    return "event-" + e.type.toLowerCase()
  }

  deletePosition() {
    // prompt user to confirm
    if (confirm("Are you sure you want to delete this position?")) {
      this.stockService.deletePosition(this._position.positionId)
        .subscribe(
          (_) => {
            this._position = null
            this.positionDeleted.emit()
          },
          err => {
            let errors = GetErrors(err)
            alert("Error deleting position: " + errors.join(", "))
          })
    }
  }

  closePosition() {
    if (confirm("Are you sure you want to close this position?")) {
      this.stockService.closePosition(this._position.positionId)
        .subscribe(
          (_) => {
            this.brokerageOrdersChanged.emit()
          },
          err => {
            let errors = GetErrors(err)
            alert("Error closing position: " + errors.join(", "))
          })
    }
  }


  deleteTransaction(transactionId: string) {
    if (confirm("are you sure you want to delete the transaction?")) {
      this.stockService.deleteTransaction(this._position.positionId, transactionId)
        .subscribe(
          _ => {
            // refresh UI somehow here, tbd
          }, (err) => {
            var errors = GetErrors(err)
            alert("Error deleting transaction: " + errors.join(", "))
          })
    }
  }

  clearStrategy(elementVisibilityToToggle:HTMLElement[]) {
    if (!confirm("Are you sure you want to clear the strategy?")) {
      return false
    }

    this.stockService.deleteLabel(this._position.positionId, "strategy").subscribe(
      _ => {
        this.positionStrategy = null
        elementVisibilityToToggle.forEach(this.toggleVisibility)
      },
      (err) => {
          const errors = GetErrors(err)
          alert("Error clearing strategy: " + errors.join(", "))
      }
    )

    return false
  }

  setStrategy(strategy: string, elementVisibilityToToggle:HTMLElement[]) {
    if (!strategy) {
      alert("Please select strategy")
      return
    }

    let label = {
      key: "strategy",
      value: strategy
    }

    this.stockService.setLabel(this._position.positionId, label).subscribe(
      _ => {
        this.positionStrategy = strategy
        elementVisibilityToToggle.forEach(this.toggleVisibility)
      },
        (err) => {
            const errors = GetErrors(err)
            alert("Error setting strategy: " + errors.join(", "))
        }
    )
  }

  toggleVisibility(elem) {
    toggleVisuallyHidden(elem)
  }

  getPrice() {
    if (!this.quote) {
      return null
    }

    return this.quote.price
  }

  getUnrealizedProfit() {
    if (!this.quote) {
      return null
    }

    return this._position.profit + (this.quote.price - this._position.averageCostPerShare) * this._position.numberOfShares
  }

  getUnrealizedGainPct() {
    if (!this.quote) {
      return null
    }
    return (this.quote.price - this._position.averageCostPerShare) / this._position.averageCostPerShare
  }
}

