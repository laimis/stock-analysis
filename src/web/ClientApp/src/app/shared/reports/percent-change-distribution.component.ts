import { Component, Input } from '@angular/core';
import {DataPoint, DataPointContainer, StockPercentChangeResponse} from '../../services/stocks.service';

@Component({
  selector: 'app-percent-change-distribution',
  templateUrl: './percent-change-distribution.component.html',
  styleUrls: ['./percent-change-distribution.component.css']
})
export class PercentChangeDistributionComponent {

  error: string = null;
  data: StockPercentChangeResponse;
  recentData: DataPoint[];
  allTimeData: DataPoint[];

  @Input()
  set percentChangeDistribution(value: StockPercentChangeResponse) {
    if (!value) {
      return
    }

    this.data = value

    this.recentData = value.recent.buckets.map(b => {
      return {
        value: b.frequency,
        label: b.value.toString()
      }
    });

    this.allTimeData = value.allTime.buckets.map(b => {
      return {
        value: b.frequency,
        label: b.value.toString()
      }
    });
  }


}
