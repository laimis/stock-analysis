import { Component, Input } from '@angular/core';
import { Evaluations, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-analysis-report',
  templateUrl: './outcomes-analysis-report.component.html',
  styleUrls: ['./outcomes-analysis-report.component.css']
})
export class OutcomesAnalysisReportComponent {

  @Input()
  report: Evaluations

  @Input()
  title: string

  @Input()
  showSummary: boolean
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
  }
}
