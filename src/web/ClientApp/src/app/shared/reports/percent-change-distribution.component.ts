import { Component, Input } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { StockPercentChangeResponse } from '../../services/stocks.service';

@Component({
  selector: 'app-percent-change-distribution',
  templateUrl: './percent-change-distribution.component.html',
  styleUrls: ['./percent-change-distribution.component.css']
})
export class PercentChangeDistributionComponent {
  
  error: string = null;
  data: StockPercentChangeResponse;

  @Input()
  set percentChangeDistribution(value: StockPercentChangeResponse) {
    this.data = value

    this.recentDataLabels = value.recent.buckets.map(b => b.percentChange.toString());
    this.recentData = [
      {
        data: value.recent.buckets.map(b => b.frequency),
        label: "Frequency",
        fill: false
      }]

    this.allTimeDataLabels = value.allTime.buckets.map(b => b.percentChange.toString());
    this.allTimeData = [
      {
        data: value.allTime.buckets.map(b => b.frequency),
        label: "Frequency",
        fill: false
      }]
  }

  public lineChartPlugins = [];
  public chartType : ChartType = 'bar';
  public lineChartLegend = true;
  public recentData: ChartDataset[] = [];
  public recentDataLabels: string[] = [];
  public allTimeData: ChartDataset[] = [];
  public allTimeDataLabels: string[] = [];
  public chartOptions: ChartOptions = {
    responsive: true,
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };
  
}
