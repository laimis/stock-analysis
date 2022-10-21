import { Component, Input } from '@angular/core';
import { OutcomesAnalysisReport, TickerOutcomes } from '../../services/stocks.service';

@Component({
  selector: 'app-portfolio-report',
  templateUrl: './portfolio-report.component.html',
  styleUrls: ['./portfolio-report.component.css']
})
export class PortfolioReportComponent {

  @Input()
  report: OutcomesAnalysisReport

  @Input()
  title: string
  
	getKeys(entries:TickerOutcomes[]) {
    return entries[0].outcomes.map(o => o.key)
  }
}
