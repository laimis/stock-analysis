import {CurrencyPipe, DecimalPipe, PercentPipe} from '@angular/common';
import {Component, Input} from '@angular/core';
import {
  AnalysisOutcomeEvaluation,
  OutcomeValueTypeEnum,
  StockAnalysisOutcome,
  TickerOutcomes
} from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes',
  templateUrl: './outcomes.component.html',
  styleUrls: ['./outcomes.component.css'],
  providers: [PercentPipe, CurrencyPipe, DecimalPipe]
})
export class OutcomesComponent {

  constructor(
    private percentPipe:PercentPipe,
    private currencyPipe:CurrencyPipe,
    private decimalPipe:DecimalPipe) { }

  @Input()
  title: string

  @Input()
  outcomes: TickerOutcomes[]

  @Input()
  tickerFilter: string

  @Input()
  set category(value:AnalysisOutcomeEvaluation) {
    // make a copy of matching tickers so we can sort it
    this.outcomes = value.matchingTickers.map(t => {
      return {
        ticker: t.ticker,
        outcomes: t.outcomes
      }
    })
    this.highlightColumn = value.sortColumn
    this.sort(value.sortColumn)
  }

  sortColumn: string;
  highlightColumn: string;
  sortDirection: number = -1

	getKeys(entries:TickerOutcomes[]) {
    if (entries === null || entries.length === 0) {
      return []
    }

    let otherColumns = entries[0].outcomes
      .filter(o => this.IsRenderableOutcome(o) && o.key !== this.highlightColumn)
      .map(o => o.key)

    if (this.highlightColumn === undefined) {
      return otherColumns
    }

    return [this.highlightColumn, ...otherColumns]
  }

  sort(column:string) {

    var func = this.getSortFunc(column);

    if (this.sortColumn != column) {
      this.sortDirection = -1
    } else {
      this.sortDirection *= -1
    }
    this.sortColumn = column

    var finalFunc = (a, b) => {
      var result = func(a, b)
      return result * this.sortDirection
    }

    this.runSort(this.outcomes, finalFunc)
  }

  outcomesForRendering(outcomes:StockAnalysisOutcome[]) {

    const highlightColumn = outcomes.find(o => o.key === this.highlightColumn);
    const others = outcomes.filter(o => o.key !== this.highlightColumn);

    return [highlightColumn, ...others]
      .filter(o => this.IsRenderableOutcome(o))
  }

  private IsRenderableOutcome(o: StockAnalysisOutcome): unknown {
    return o !== undefined && o.key !== 'NewHigh' && o.key !== 'NewLow';
  }

  private runSort(analysis:TickerOutcomes[], compareFn) {
    analysis.sort(compareFn)
  }

  private getSortFunc(column:string) {
    if (column === 'ticker') {
      return (a:TickerOutcomes, b:TickerOutcomes) => {
        return a.ticker.localeCompare(b.ticker)
      }
    }
    else {
      return (a:TickerOutcomes, b:TickerOutcomes) => {
        if (a.outcomes.find(o => o.key === column) === undefined) {
          return 0
        }

        if (b.outcomes.find(o => o.key === column) === undefined) {
          return 0
        }

        const aVal = a.outcomes.find(o => o.key === column).value;
        const bVal = b.outcomes.find(o => o.key === column).value;

        return aVal - bVal
      }
    }

  }

  getValue(o:StockAnalysisOutcome) {
    if (o.valueType === OutcomeValueTypeEnum.Percentage) {
      return this.percentPipe.transform(o.value, '1.0-2')
    } else if (o.valueType === OutcomeValueTypeEnum.Currency) {
      return this.currencyPipe.transform(o.value)
    } else {
      return this.decimalPipe.transform(o.value)
    }
  }

  async copyOutcomesToClipboard() {
    let data =
      this.outcomes
        .map(o => {
          return `${o.ticker},${o.outcomes.filter(o => o.key ==='Gain').map(o => this.getValue(o).replace(",", "")).join(',')}`
        })

    // let header = `Ticker,${this.getKeys(this.outcomes).join(',')}`
    let header = `Ticker,Gain`
    data.unshift(header)

    let text = data.join('\n')

    await navigator.clipboard.writeText(text)
  }
}
