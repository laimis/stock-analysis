import { Component, Input } from '@angular/core';
import { AnalysisCategoryGrouping, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes',
  templateUrl: './outcomes.component.html',
  styleUrls: ['./outcomes.component.css']
})
export class OutcomesComponent {

  @Input()
  title: string
  
  @Input()
  outcomes: TickerOutcomes[]

  @Input()
  set category(value:AnalysisCategoryGrouping) {
    this.outcomes = value.outcomes
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
}
