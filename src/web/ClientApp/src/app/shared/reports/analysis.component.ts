import { Component, Input } from '@angular/core';
import { PortfolioReportCategory, PositionAnalysisEntry } from '../../services/stocks.service';

@Component({
  selector: 'app-analysis',
  templateUrl: './analysis.component.html',
  styleUrls: ['./analysis.component.css']
})
export class AnalysisComponent {

  @Input()
  title: string
  
  @Input()
  analysis: PositionAnalysisEntry[]

  @Input()
  set category(value:PortfolioReportCategory) {
    this.analysis = value.analysis
    this.sort(value.sortColumn)
  }

  sortColumn: string;
  sortDirection: number = -1
  
	getKeys(entries:PositionAnalysisEntry[]) {
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

    this.runSort(this.analysis, finalFunc)
  }

  private runSort(analysis:PositionAnalysisEntry[], compareFn) {
    analysis.sort(compareFn)
  }

  private getSortFunc(column:string) {
    return (a:PositionAnalysisEntry, b:PositionAnalysisEntry) => {
      var aVal = a.outcomes.find(o => o.key === column).value
      var bVal = b.outcomes.find(o => o.key === column).value

      return aVal - bVal
    }
  }
}
