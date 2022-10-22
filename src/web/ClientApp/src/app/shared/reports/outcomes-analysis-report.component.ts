import { Component, Input } from '@angular/core';
import { OutcomesAnalysisReport, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-outcomes-analysis-report',
  templateUrl: './outcomes-analysis-report.component.html',
  styleUrls: ['./outcomes-analysis-report.component.css']
})
export class OutcomesAnalysisReportComponent {

  @Input()
  report: OutcomesAnalysisReport

  @Input()
  title: string
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
  }
}
