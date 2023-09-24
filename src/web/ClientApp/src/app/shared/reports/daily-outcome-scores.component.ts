import { Component, Input } from '@angular/core';
import {DailyScore, DataPoint} from '../../services/stocks.service';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {
  data: DataPoint[] = [];

  @Input()
  set dailyScores(values:DailyScore[]) {
    this.data = values.map(x => {
      return {
        label: x.date,
        value: x.score
      }
    })
  }

  @Input()
  errors: string[];
}
