import { Component, Input } from '@angular/core';
import { DailyScore } from '../../services/stocks.service';
import { ChartType } from 'chart.js';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {
  dailyScores: number[];
  dailyScoresDates: string[];
  
  @Input()
  set setDailyScores(scores:DailyScore[]) {
    this.dailyScores = scores.map(d => d.score)
    this.dailyScoresDates = scores.map(d => d.date.split('T')[0])
  }

  @Input()
  chartType: ChartType = 'bar';

  @Input()
  errors: string[];
}
