import { Component, Input } from '@angular/core';
import { AnalysisOutcomeEvaluation, OutcomesReport, TickerOutcomes } from '../../services/stocks.service';
import { charts_getTradingViewLink } from '../../services/links.service';

@Component({
  selector: 'app-outcomes-analysis-report',
  templateUrl: './outcomes-analysis-report.component.html',
  styleUrls: ['./outcomes-analysis-report.component.css']
})
export class OutcomesAnalysisReportComponent {
  private _report: OutcomesReport;

  @Input()
  set report(value: OutcomesReport) {
    this.selectedEvaluationName = value.evaluations.length > 0 ? value.evaluations[0].name : null
    this._report = value
  }
  get report(): OutcomesReport {
    return this._report
  }

  @Input()
  title: string

  @Input()
  showSummary: boolean

  @Input()
  tickerFilter: string

  private selectedEvaluationName: string = null

  toggleEvaluation(evaluation:string) {
    if (this.selectedEvaluationName === evaluation) {
      this.selectedEvaluationName = null
    } else {
      this.selectedEvaluationName = evaluation
    }
  }
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
  }

  hasPatterns(report: OutcomesReport) {
    return report.patterns.some(t => t.patterns.length > 0)
  }

  tradingViewLink(ticker:string) {
    return charts_getTradingViewLink(ticker)
  }

  copyTickersToClipboard(c:AnalysisOutcomeEvaluation) {
    var tickers = c.matchingTickers.map(t => t.ticker)
    var text = tickers.join('\r')
    navigator.clipboard.writeText(text)
  }

  isSelected(evaluation:string) {
    return this.selectedEvaluationName === null ||
      this.selectedEvaluationName === evaluation
  }
}
