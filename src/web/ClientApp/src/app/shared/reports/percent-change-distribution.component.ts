import { Component, Input } from '@angular/core';
import {ChartType, DataPoint, DataPointContainer, StockPercentChangeResponse} from '../../services/stocks.service';

@Component({
  selector: 'app-percent-change-distribution',
  templateUrl: './percent-change-distribution.component.html',
  styleUrls: ['./percent-change-distribution.component.css']
})
export class PercentChangeDistributionComponent {

  error: string = null;
  data: StockPercentChangeResponse;
  recentData: DataPointContainer;
  allTimeData: DataPointContainer;

  @Input()
  set percentChangeDistribution(value: StockPercentChangeResponse) {
    if (!value) {
      return
    }

    this.data = value

    this.recentData = {
      data: value.recent.buckets.map(b => {
        return {
          label: b.value.toString(),
          value: b.frequency,
          isDate: false
        }
      }),
      label: "Recent",
      chartType: ChartType.Column,
      annotationLine: null
    };

    this.allTimeData = {
      data: value.allTime.buckets.map(b => {
        return {
          label: b.value.toString(),
          value: b.frequency,
          isDate: false
        }
      }),
      label: "All time",
      chartType: ChartType.Column,
      annotationLine: null
    };
  }

}
