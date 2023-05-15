import { Component, Input } from '@angular/core';
import { AnalysisOutcomeEvaluation, OutcomesReport, TickerCountPair, TickerOutcomes, TickerPatterns } from '../../services/stocks.service';
import { charts_getTradingViewLink } from '../../services/links.service';

@Component({
  selector: 'app-outcomes-analysis-report',
  templateUrl: './outcomes-analysis-report.component.html',
  styleUrls: ['./outcomes-analysis-report.component.css']
})
export class OutcomesAnalysisReportComponent {
  private _report: OutcomesReport;
  tickersForSummary: TickerCountPair[] = []
  patterns: TickerPatterns[] = []

  @Input()
  set report(value: OutcomesReport) {
    this.selectedEvaluationName = value.evaluations.length > 0 ? value.evaluations[0].name : null
    this.tickersForSummary = value.tickerSummary
    this.patterns = value.patterns
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

  @Input()
  set excludeTickers(value:string[]) {
    this.tickersForSummary = this._report.tickerSummary.filter(t => !value.includes(t.ticker))
    this.patterns = this._report.patterns.filter(t => !value.includes(t.ticker))
  }

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

  hasPatterns(patterns: TickerPatterns[]) {
    return patterns.some(t => t.patterns.length > 0)
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
