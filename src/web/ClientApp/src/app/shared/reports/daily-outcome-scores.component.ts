import { Component, Input } from '@angular/core';
import { DailyScore } from '../../services/stocks.service';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {

  @Input()
  dailyScores:DailyScore[]

  @Input()
  errors: string[];
}
