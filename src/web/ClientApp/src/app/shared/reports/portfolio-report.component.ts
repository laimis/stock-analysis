import { Component, Input } from '@angular/core';
import { PortfolioReport, PositionAnalysisEntry } from '../../services/stocks.service';

@Component({
  selector: 'app-portfolio-report',
  templateUrl: './portfolio-report.component.html',
  styleUrls: ['./portfolio-report.component.css']
})
export class PortfolioReportComponent {

  @Input()
  report: PortfolioReport

  @Input()
  title: string
  
	getKeys(entries:PositionAnalysisEntry[]) {
    return entries[0].outcomes.map(o => o.key)
  }
}
