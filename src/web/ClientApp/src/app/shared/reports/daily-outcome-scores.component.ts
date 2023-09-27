import { Component, Input } from '@angular/core';
import {DataPointContainer} from '../../services/stocks.service';

@Component({
  selector: 'app-daily-outcome-scores',
  templateUrl: './daily-outcome-scores.component.html',
  styleUrls: ['./daily-outcome-scores.component.css']
})
export class DailyOutcomeScoresComponent {

  @Input()
  dailyScores:DataPointContainer

  @Input()
  errors: string[];
}
