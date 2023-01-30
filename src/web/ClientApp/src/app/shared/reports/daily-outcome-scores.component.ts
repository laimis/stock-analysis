import { Component, Input } from '@angular/core';
import { DailyOutcomeScoresReport } from '../../services/stocks.service';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {
  
  @Input()
  dailyOutcomeScoresReport: DailyOutcomeScoresReport
  
  getDailyScores() {
    return this.dailyOutcomeScoresReport.dailyScores.map(d => d.score)
  }

  getDailyScoresDates() {
    return this.dailyOutcomeScoresReport.dailyScores.map(d => d.date.split('T')[0])
  }
}
