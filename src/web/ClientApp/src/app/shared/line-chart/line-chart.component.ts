import {Component, Input} from '@angular/core';
import {createChart, IChartApi, ISeriesApi} from "lightweight-charts";
import {DataPoint} from "../../services/stocks.service";

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html',
  styleUrls: ['./line-chart.component.css']
})
export class LineChartComponent {
  chart: IChartApi;

  @Input()
  chartHeight: number = 400;
  private _values: DataPoint[];

  @Input()
  set data(values: DataPoint[]) {
    if (values) {
      this._values = values
      this.renderChart();
    }
  }

  @Input()
  chartType = 'line'; // can also be 'bar'

  private removeChart() {
    if (this.chart) {
      this.chart.remove();
    }
  }

  private assignData(series:ISeriesApi<any>) {
    series.setData(this._values.map(v => {
      let time = v.label.indexOf('T') > -1 ? v.label.split('T')[0] : v.label;
      return {time: time, value: v.value}
    }));
  }

  private renderChart() {
    this.removeChart();

    this.chart = createChart(
      document.getElementById('chart'),
      {
        height: this.chartHeight,
        handleScale: {
          axisPressedMouseMove: false
        },
        handleScroll: {
          mouseWheel: false,
          pressedMouseMove: false,
          vertTouchDrag: false
        }
      }
    );

    let series =
      this.chartType == 'line'
        ? this.chart.addLineSeries() :
        this.chart.addBarSeries();

    this.assignData(series);

    this.chart.timeScale().fitContent();
  }
}
