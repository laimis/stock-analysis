import { CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AnalysisOutcomeEvaluation, OutcomeValueTypeEnum, StockAnalysisOutcome, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes',
  templateUrl: './outcomes.component.html',
  styleUrls: ['./outcomes.component.css'],
  providers: [PercentPipe, CurrencyPipe]
})
export class OutcomesComponent {

  constructor(
    private percentPipe:PercentPipe,
    private currencyPipe:CurrencyPipe) { }

  @Input()
  title: string
  
  @Input()
  outcomes: TickerOutcomes[]

  @Input()
  set category(value:AnalysisOutcomeEvaluation) {
    this.outcomes = value.matchingTickers
    this.sort(value.sortColumn)
  }

  sortColumn: string;
  sortDirection: number = -1
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
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

  private runSort(analysis:TickerOutcomes[], compareFn) {
    analysis.sort(compareFn)
  }

  private getSortFunc(column:string) {
    return (a:TickerOutcomes, b:TickerOutcomes) => {
      var aVal = a.outcomes.find(o => o.key === column).value
      var bVal = b.outcomes.find(o => o.key === column).value

      return aVal - bVal
    }
  }

  getValue(o:StockAnalysisOutcome) {
    if (o.valueType === OutcomeValueTypeEnum.Percentage) {
      return this.percentPipe.transform(o.value)
    } else if (o.valueType === OutcomeValueTypeEnum.Currency) {
      return this.currencyPipe.transform(o.value)
    } else {
      return o.value
    }
  }
}
