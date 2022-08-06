import { Component, OnInit, Input } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { DataPoint, DataPointContainer, StockTradingPerformance } from 'src/app/services/stocks.service';


@Component({
  selector: 'stock-trading-performance',
  templateUrl: './stock-trading-performance.component.html',
  styleUrls: ['./stock-trading-performance.component.css']
})
export class StockTradingPerformanceComponent implements OnInit {

  ngOnInit() {
  }

  @Input()
  performance: StockTradingPerformance

  @Input()
  trends : DataPointContainer[]

  getData(c:DataPointContainer) {
    var data = [
      {
        data: c.data.map(x => x.value),
        label: c.label,
        fill: false,
        tension: 0.1,
        pointRadius: 1,
        borderWidth: 1,
        pointStyle: 'line'
      }]

    return data
  }

  getLabels(c:DataPointContainer) {
    return c.data.map(x => x.label)
  }

  public lineChartPlugins = [];
  public lineChartType : ChartType = 'line';
  public lineChartLegend = true;
  public lineChartOptions: ChartOptions = {
    responsive: true,
    scales: {
      y: {
        display: true,
      }
    },
    plugins: {
      annotation: {
        annotations: []
      }
    }
  };

}
