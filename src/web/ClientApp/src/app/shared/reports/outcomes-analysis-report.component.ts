import { Component, Input } from '@angular/core';
import { OutcomesReport, TickerOutcomes } from '../../services/stocks.service';
import { charts_getTradingViewLink } from '../../services/links.service';

@Component({
  selector: 'app-outcomes-analysis-report',
  templateUrl: './outcomes-analysis-report.component.html',
  styleUrls: ['./outcomes-analysis-report.component.css']
})
export class OutcomesAnalysisReportComponent {

  @Input()
  report: OutcomesReport

  @Input()
  title: string

  @Input()
  showSummary: boolean

  @Input()
  tickerFilter: string
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
  }

  hasPatterns(report: OutcomesReport) {
    return report.patterns.some(t => t.patterns.length > 0)
  }

  tradingViewLink(ticker:string) {
    return charts_getTradingViewLink(ticker)
  }
}
