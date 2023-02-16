import { Component, Input } from '@angular/core';
import { DailyOutcomeScoresReport } from '../../services/stocks.service';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {
  dailyScores: number[];
  dailyScoresDates: string[];
  
  @Input()
  set dailyOutcomeScoresReport(report:DailyOutcomeScoresReport) {
    console.log('report set: ' + report)
    this.dailyScores = report.dailyScores.map(d => d.score)
    this.dailyScoresDates = report.dailyScores.map(d => d.date.split('T')[0])
  }
}
